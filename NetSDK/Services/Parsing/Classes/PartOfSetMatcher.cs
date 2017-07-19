﻿using Splitio.Domain;
using Splitio.Services.Client.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splitio.Services.Parsing
{
    public class PartOfSetMatcher : BaseMatcher, IMatcher
    {
        private HashSet<string> itemsToCompare = new HashSet<string>();

        public PartOfSetMatcher(List<string> compareTo)
        {
            if (compareTo != null)
            {
                itemsToCompare.UnionWith(compareTo);
            }
        }


        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, ISplitClient splitClient = null)
        {
            if (key == null || key.Count == 0)
            {
                return false;
            }
            
            return key.All(k => itemsToCompare.Contains(k));
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, ISplitClient splitClient = null)
        {
            return false;
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, ISplitClient splitClient = null)
        {
            return false;
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, ISplitClient splitClient = null)
        {
            return false;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, ISplitClient splitClient = null)
        {
            return false;
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, ISplitClient splitClient = null)
        {
            return false;
        }
    }
}
