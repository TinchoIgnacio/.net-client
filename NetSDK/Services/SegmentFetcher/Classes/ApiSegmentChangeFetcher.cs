﻿using Splitio.Domain;
using Splitio.Services.SplitFetcher.Interfaces;
using Newtonsoft.Json;
using Splitio.Services.SegmentFetcher.Interfaces;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class ApiSegmentChangeFetcher: SegmentChangeFetcher, ISegmentChangeFetcher
    {
        private readonly ISegmentSdkApiClient apiClient;

        public ApiSegmentChangeFetcher(ISegmentSdkApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        protected override SegmentChange FetchFromBackend(string name, long since)
        {
            var fetchResult = apiClient.FetchSegmentChanges(name, since);

            var segmentChange = JsonConvert.DeserializeObject<SegmentChange>(fetchResult);
            return segmentChange;
        }
    }
}
