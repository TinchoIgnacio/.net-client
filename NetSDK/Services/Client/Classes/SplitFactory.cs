﻿using NLog;
using NLog.Config;
using NLog.Targets;
using Splitio.Services.Client.Interfaces;
using System;
using System.Linq;

namespace Splitio.Services.Client.Classes
{
    public class SplitFactory
    {
        private ISplitClient client;
        private ISplitManager manager;
        private string apiKey;
        private ConfigurationOptions options;

        public SplitFactory(string apiKey, ConfigurationOptions options = null)
        {
            this.apiKey = apiKey;
            this.options = options;
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            var fileTarget = new FileTarget();
            fileTarget.Name = "splitio";
            fileTarget.FileName = @".\Logs\splitio.log";
            fileTarget.ArchiveFileName = @".\Logs\splitio.{#}.log";
            fileTarget.LineEnding = LineEndingMode.CRLF;
            fileTarget.Layout = "${longdate} ${level: uppercase = true} ${logger} - ${message} - ${exception:format=tostring}";
            fileTarget.ConcurrentWrites = true;
            fileTarget.CreateDirs = true;
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.DateAndSequence;
            fileTarget.ArchiveAboveSize = 200000000;
            fileTarget.ArchiveDateFormat = "yyyyMMdd";
            fileTarget.MaxArchiveFiles = 30;
            var rule = new LoggingRule("*", LogLevel.Debug, fileTarget);

            if (LogManager.Configuration == null)
            {
                var config = new LoggingConfiguration();
                config.AddTarget("splitio", fileTarget);
                config.LoggingRules.Add(rule);
                LogManager.Configuration = config;
            }
            else
            {
                if (LogManager.Configuration.ConfiguredNamedTargets.Where(x => x.Name == "splitio").FirstOrDefault() == null)
                {
                    LogManager.Configuration.AddTarget("splitio", fileTarget);
                    LogManager.Configuration.LoggingRules.Add(rule);
                }
            }
        }

        public ISplitClient Client()
        {
            if (client == null)
            {
                BuildSplitClient();
            }
            return client;
        }

        private void BuildSplitClient()
        {
            if (options == null)
            {
                options = new ConfigurationOptions();
            }

            switch(options.Mode)
            {
                case Mode.Standalone:
                    if (String.IsNullOrEmpty(apiKey))
                    {
                        throw new Exception("API Key should be set to initialize Split SDK.");
                    }
                    if (apiKey == "localhost")
                    {
                        client = new LocalhostClient(options.LocalhostFilePath);
                    }
                    else
                    {
                        client = new SelfRefreshingClient(apiKey, options);
                    }
                    break;
                case Mode.Consumer:
                    if (options.CacheAdapterConfig != null && options.CacheAdapterConfig.Type == AdapterType.Redis)
                    {
                        if (String.IsNullOrEmpty(options.CacheAdapterConfig.Host) || String.IsNullOrEmpty(options.CacheAdapterConfig.Port) || String.IsNullOrEmpty(options.CacheAdapterConfig.Password))
                        {
                            throw new Exception("Redis Host, Port and Password should be set to initialize Split SDK in Redis Mode.");
                        }
                        client = new RedisClient(options);
                    }
                    else
                    {
                        throw new Exception("Redis config should be set to build split client in Consumer mode.");
                    }
                    break;
                case Mode.Producer:
                    throw new Exception("Unsupported mode.");
                default:
                    throw new Exception("Mode should be set to build split client.");
            }
            

            
        }

        public ISplitManager Manager()
        {
            if (client == null)
            {
                BuildSplitClient();
            }
           
            manager = client.GetSplitManager();

            return manager;
        }
    }
}
