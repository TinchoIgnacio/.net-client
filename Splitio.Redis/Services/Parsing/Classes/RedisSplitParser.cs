﻿using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splitio.Redis.Services.Parsing.Classes
{
    public class RedisSplitParser : SplitParser
    {
        public RedisSplitParser(ISegmentCache segmentsCache)
        {
            this.segmentsCache = segmentsCache;
        }

        protected override IMatcher GetInSegmentMatcher(MatcherDefinition matcherDefinition, ParsedSplit parsedSplit)
        {
            var matcherData = matcherDefinition.userDefinedSegmentMatcherData;
            return new UserDefinedSegmentMatcher(matcherData.segmentName, segmentsCache);
        }
    }
}
