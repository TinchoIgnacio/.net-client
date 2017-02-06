﻿using Splitio.Services.Cache.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splitio.Services.Cache.Classes
{
    public abstract class RedisCacheBase
    {
        private const string RedisKeyPrefixFormat = "SPLITIO/{sdk-language-version}/{instance-id}/";
        protected IRedisAdapter redisAdapter;
        protected string redisKeyPrefix;

        public RedisCacheBase(IRedisAdapter redisAdapter, string machineIP, string sdkLanguage, string sdkVersion)
        {
            this.redisAdapter = redisAdapter;
            this.redisKeyPrefix = RedisKeyPrefixFormat.Replace("{sdk-language-version}", sdkLanguage + "-" + sdkVersion)
                                                      .Replace("{instance-id}", machineIP);
        }
    }
}