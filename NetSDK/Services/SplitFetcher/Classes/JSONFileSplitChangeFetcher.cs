﻿using Splitio.Domain;
using Newtonsoft.Json;
using System.IO;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class JSONFileSplitChangeFetcher : SplitChangeFetcher, ISplitChangeFetcher 
    {
        public ISplitCache splitCache { get; private set; }
        private string filePath;
        public JSONFileSplitChangeFetcher(string filePath)
        {
            this.filePath = filePath;
        }

        protected override SplitChangesResult FetchFromBackend(long since)
        {
            var json = File.ReadAllText(filePath);
            var splitChangesResult = JsonConvert.DeserializeObject<SplitChangesResult>(json);
            return splitChangesResult;
        }
    }
}
