﻿using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Metrics.Classes;
using Splitio.Services.Metrics.Interfaces;
using Splitio.Services.Parsing;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Classes;
using System.Net.Sockets;

namespace Splitio.Services.Client.Classes
{
    public class SelfRefreshingClient: SplitClient
    {
        private static string ApiKey;
        private static string BaseUrl;
        private static int SplitsRefreshRate;
        private static int SegmentRefreshRate;
        private static string HttpEncoding;
        private static long HttpConnectionTimeout;
        private static long HttpReadTimeout;
        private static string SdkVersion;
        private static string SdkSpecVersion;
        private static string SdkMachineName;
        private static string SdkMachineIP;
        private static bool RandomizeRefreshRates;
        private static int BlockMilisecondsUntilReady;
        private static int ConcurrencyLevel;
        private static int TreatmentLogRefreshRate;
        private static int TreatmentLogSize;
        private static string EventsBaseUrl;
        private static int MaxCountCalls;
        private static int MaxTimeBetweenCalls;
        private static int NumberOfParalellSegmentTasks;

        /// <summary>
        /// Represents the initial number of buckets for a ConcurrentDictionary. 
        /// Should not be divisible by a small prime number. 
        /// The default capacity is 31. 
        /// More details : https://msdn.microsoft.com/en-us/library/dd287171(v=vs.110).aspx
        /// </summary>
        private const int InitialCapacity = 31;


        private IReadinessGatesCache gates;
        private SelfRefreshingSplitFetcher splitFetcher;
        private ISplitSdkApiClient splitSdkApiClient;
        private ISegmentSdkApiClient segmentSdkApiClient;
        private ITreatmentSdkApiClient treatmentSdkApiClient;
        private IMetricsSdkApiClient metricsSdkApiClient;
        private SelfRefreshingSegmentFetcher selfRefreshingSegmentFetcher;
        private IImpressionListener treatmentLog;

        public SelfRefreshingClient(string apiKey, ConfigurationOptions config)
        {
            InitializeLogger();
            ApiKey = apiKey;
            ReadConfig(config);
            BuildSdkReadinessGates();
            BuildSdkApiClients();
            BuildSplitFetcher();
            BuildTreatmentLog(config);
            BuildSplitter();
            BuildManager();
            Start();
            if (BlockMilisecondsUntilReady > 0)
            {
                BlockUntilReady(BlockMilisecondsUntilReady);
            }
            LaunchTaskSchedulerOnReady();
        }

        private void ReadConfig(ConfigurationOptions config)
        {
            BaseUrl = String.IsNullOrEmpty(config.Endpoint) ? "https://sdk.split.io" : config.Endpoint;
            EventsBaseUrl = String.IsNullOrEmpty(config.EventsEndpoint) ? "https://events.split.io" : config.EventsEndpoint;
            SplitsRefreshRate = config.FeaturesRefreshRate ?? 60;
            SegmentRefreshRate = config.SegmentsRefreshRate ?? 60;
            HttpEncoding = "gzip";
            HttpConnectionTimeout = config.ConnectionTimeout ?? 15000;
            HttpReadTimeout = config.ReadTimeout ?? 15000;
            SdkVersion = ".NET-" + Version.SplitSdkVersion;
            SdkSpecVersion = ".NET-" + Version.SplitSpecVersion;
            SdkMachineName = config.SdkMachineName ?? Environment.MachineName;
            SdkMachineIP = config.SdkMachineIP ?? Dns.GetHostAddresses(Environment.MachineName).Where(x => x.AddressFamily == AddressFamily.InterNetwork && x.IsIPv6LinkLocal == false).Last().ToString();
            RandomizeRefreshRates = config.RandomizeRefreshRates;
            BlockMilisecondsUntilReady = config.Ready ?? 0;
            ConcurrencyLevel = config.SplitsStorageConcurrencyLevel ?? 4;
            TreatmentLogRefreshRate = config.ImpressionsRefreshRate ?? 30;
            TreatmentLogSize = config.MaxImpressionsLogSize ?? 30000;
            MaxCountCalls = config.MaxMetricsCountCallsBeforeFlush ?? 1000;
            MaxTimeBetweenCalls = config.MetricsRefreshRate ?? 60;
            NumberOfParalellSegmentTasks = config.NumberOfParalellSegmentTasks ?? 5;
            LabelsEnabled = config.LabelsEnabled ?? true;
        }

        private void BlockUntilReady(int BlockMilisecondsUntilReady)
        {
            if (!gates.IsSDKReady(BlockMilisecondsUntilReady))
            {
                throw new TimeoutException(String.Format("SDK was not ready in {0} miliseconds", BlockMilisecondsUntilReady));
            }
        }

        public void Start()
        {
            ((SelfUpdatingTreatmentLog)treatmentLog).Start();
            ((SelfRefreshingSplitFetcher)splitFetcher).Start();
        }

        private void LaunchTaskSchedulerOnReady()
        {
            Task workerTask = Task.Factory.StartNew(
                () => {
                    while (true)
                    {
                        if (gates.IsSDKReady(0))
                        {                           
                            selfRefreshingSegmentFetcher.StartScheduler();
                            break;
                        }
                    }
                });
        }

        public void Stop()
        {
            ((SelfRefreshingSplitFetcher)splitFetcher).Stop();
            ((SelfRefreshingSegmentFetcher)selfRefreshingSegmentFetcher).Stop();
            ((SelfUpdatingTreatmentLog)treatmentLog).Stop();
        }

        private void InitializeLogger()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            if (hierarchy.Root.Appenders.Count == 0)
            {
                RollingFileAppender fileAppender = new RollingFileAppender();
                fileAppender.AppendToFile = true;
                fileAppender.LockingModel = new FileAppender.MinimalLock();
                fileAppender.File = @"Logs\split-sdk.log";
                fileAppender.RollingStyle = RollingFileAppender.RollingMode.Date;
                fileAppender.DatePattern = "yyyyMMdd";
                PatternLayout pl = new PatternLayout();
                pl.ConversionPattern = "%date %level %logger - %message%newline";
                pl.ActivateOptions();
                fileAppender.Layout = pl;
                fileAppender.ActivateOptions();

                log4net.Config.BasicConfigurator.Configure(fileAppender);
            }
        }

        private void BuildSplitter()
        {
            splitter = new Splitter();
        }

        private void BuildSdkReadinessGates()
        {
            gates = new InMemoryReadinessGatesCache();
        }

        private void BuildSplitFetcher()
        {
            var segmentRefreshRate = RandomizeRefreshRates ? Random(SegmentRefreshRate) : SegmentRefreshRate;
            var splitsRefreshRate = RandomizeRefreshRates ? Random(SplitsRefreshRate) : SplitsRefreshRate;

            segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(ConcurrencyLevel, InitialCapacity));
            var segmentChangeFetcher = new ApiSegmentChangeFetcher(segmentSdkApiClient);
            selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(segmentChangeFetcher, gates, segmentRefreshRate, segmentCache, NumberOfParalellSegmentTasks);
            var splitChangeFetcher = new ApiSplitChangeFetcher(splitSdkApiClient);
            var splitParser = new InMemorySplitParser(selfRefreshingSegmentFetcher, segmentCache);
            splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(ConcurrencyLevel, InitialCapacity));
            splitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, splitParser, gates, splitsRefreshRate, splitCache);
        }

        private void BuildTreatmentLog(ConfigurationOptions config)
        {
            impressionsCache = new InMemoryImpressionsCache(new BlockingQueue<KeyImpression>(TreatmentLogSize));
            treatmentLog = new SelfUpdatingTreatmentLog(treatmentSdkApiClient, TreatmentLogRefreshRate, impressionsCache);
            impressionListener = new AsynchronousImpressionListener();
            ((AsynchronousImpressionListener)impressionListener).AddListener(treatmentLog);
            ((AsynchronousImpressionListener)impressionListener).AddListener(config.ImpressionListener);
        }


        private void BuildMetricsLog()
        {
            metricsCache = new InMemoryMetricsCache(new ConcurrentDictionary<string, Counter>(), new ConcurrentDictionary<string, ILatencyTracker>(), new ConcurrentDictionary<string, long>());
            metricsLog = new AsyncMetricsLog(metricsSdkApiClient, metricsCache, MaxCountCalls, MaxTimeBetweenCalls);
        }

        private int Random(int refreshRate)
        {
            Random random = new Random();
            return Math.Max(5, random.Next(refreshRate/2, refreshRate));
        }

        private void BuildSdkApiClients()
        {
            var header = new HTTPHeader();
            header.authorizationApiKey = ApiKey;
            header.encoding = HttpEncoding;
            header.splitSDKVersion = SdkVersion;
            header.splitSDKSpecVersion = SdkSpecVersion;
            header.splitSDKMachineName = SdkMachineName;
            header.splitSDKMachineIP = SdkMachineIP;
            metricsSdkApiClient = new MetricsSdkApiClient(header, EventsBaseUrl, HttpConnectionTimeout, HttpReadTimeout);
            BuildMetricsLog();
            splitSdkApiClient = new SplitSdkApiClient(header, BaseUrl, HttpConnectionTimeout, HttpReadTimeout, metricsLog);
            segmentSdkApiClient = new SegmentSdkApiClient(header, BaseUrl, HttpConnectionTimeout, HttpReadTimeout, metricsLog);
            treatmentSdkApiClient = new TreatmentSdkApiClient(header, EventsBaseUrl, HttpConnectionTimeout, HttpReadTimeout);
        }

        private void BuildManager()
        {
            manager = new SplitManager(splitCache);
        }
    }
}
