﻿using Common.Logging;
using Splitio.Redis.Services.Cache.Interfaces;
using StackExchange.Redis;
using System;
using System.Linq;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisAdapter : IRedisAdapter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RedisAdapter));

        private ConnectionMultiplexer redis;
        private IDatabase database;
        private IServer server;

        public RedisAdapter(string host, string port, string password = "", int databaseNumber = 0, 
            int connectTimeout = 0, int connectRetry = 0, int syncTimeout = 0)
        {
            try
            {
                var config = GetConfig(host, port, password, connectTimeout, connectRetry, syncTimeout);
                redis = ConnectionMultiplexer.Connect(config);
                database = redis.GetDatabase(databaseNumber);
                server = redis.GetServer(String.Format("{0}:{1}", host, port));
            }
            catch (Exception e)
            {
                Log.Error(String.Format("Exception caught building Redis Adapter '{0}:{1}': ", host, port), e);
            }
        }

        private static string GetConfig(string host, string port, string password, int connectTimeout, int connectRetry, int syncTimeout)
        {
            var config = String.Format("{0}:{1}, password = {2}, allowAdmin = true", host, port, password);
            
            if (connectTimeout > 0)
            {
                config += ", connectTimeout = " + connectTimeout;
            }
            
            if (connectRetry > 0)
            {
                config += ", connectRetry = " + connectRetry;
            }
            
            if (syncTimeout > 0)
            {
                config += ", syncTimeout = " + syncTimeout;
            }
            
            return config;
        }

        public bool Set(string key, string value)
        {
            try
            {
                return database.StringSet(key, value);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter Set", e);
                return false;
            }
        }

        public string Get(string key)
        {
            try
            {
                return database.StringGet(key);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter Get", e);
                return String.Empty;
            }
        }

        public RedisValue[] Get(RedisKey[] keys)
        {
            try
            {
                return database.StringGet(keys);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter Get", e);
                return new RedisValue[0];
            }
        }

        public RedisKey[] Keys(string pattern)
        {
            try
            {
                var keys = server.Keys(pattern: pattern);
                return keys.ToArray();
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter Keys", e);
                return new RedisKey[0];
            }
        }

        public bool Del(string key)
        {
            try
            {
                return database.KeyDelete(key);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter Del", e);
                return false;
            }
        }

        public long Del(RedisKey[] keys)
        {
            try
            {
                return database.KeyDelete(keys);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter Del", e);
                return 0;
            }
        }

        public bool SAdd(string key, RedisValue value)
        {
            try
            {
                return database.SetAdd(key, value);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter SAdd", e);
                return false;
            }
        }

        public long SAdd(string key, RedisValue[] values)
        {
            try
            {
                return database.SetAdd(key, values);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter SAdd", e);
                return 0;
            }
        }

        public long SRem(string key, RedisValue[] values)
        {
            try
            {
                return database.SetRemove(key, values);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter SRem", e);
                return 0;
            }
        }

        public bool SIsMember(string key, string value)
        {
            try
            {
                return database.SetContains(key, value);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter SIsMember", e);
                return false;
            }
        }

        public RedisValue[] SMembers(string key)
        {
            try
            {
                return database.SetMembers(key);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter SMembers", e);
                return new RedisValue[0];
            }
        }

        public long IcrBy(string key, long value)
        {
            try
            {
                return database.StringIncrement(key, value);
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter IcrBy", e);
                return 0;
            }
        }

        public void Flush()
        {
            try
            {
                server.FlushDatabase();
            }
            catch (Exception e)
            {
                Log.Error("Exception calling Redis Adapter Flush", e);
            }
        }
    }
}
