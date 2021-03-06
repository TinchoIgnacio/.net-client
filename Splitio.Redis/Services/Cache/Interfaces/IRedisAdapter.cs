﻿using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisAdapter
    {
        bool Set(string key, string value);

        string Get(string key);

        RedisValue[] Get(RedisKey[] keys);

        RedisKey[] Keys(string pattern);

        bool Del(string key);

        long Del(RedisKey[] keys);

        bool SAdd(string key, RedisValue value);

        long SAdd(string key, RedisValue[] values);

        long SRem(string key, RedisValue[] values);

        bool SIsMember(string key, string value);

        RedisValue[] SMembers(string key);

        long IcrBy(string key, long delta);

        void Flush();
    }
}
