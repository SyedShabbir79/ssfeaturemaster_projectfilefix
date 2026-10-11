using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;

namespace FunctionsV2
{


    public interface ICache
    {
        public class RedisWrite
        {
            public string key { get; set; }
            public object value { get; set; }
            public double? expiry { get; set; }
        }

        public class RedisRead
        {
            public string key { get; set; }
            public JToken value { get; set; }
            public bool hasValue { get; set; }
        }


        public Task<RedisWrite> CacheWrite(RedisWrite rSet);

        public Task<RedisRead> CacheRead(RedisRead rGet);
    }





    public class Cache : ICache
    {

        private readonly IConfiguration _config;
        public Cache(IConfiguration _config)
        {
            this._config = _config;
        }

        public async Task<ICache.RedisWrite> CacheWrite(ICache.RedisWrite rSet)
        {
            var redisConnection = _config["redisConnectionString"];
            string prependString = _config["redisPrependString"] ?? "LOCAL";

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnection);

            IDatabase db = redis.GetDatabase();

            if (!rSet.expiry.HasValue)
            {
                rSet.expiry = 30;
            }

            string _value = Newtonsoft.Json.JsonConvert.SerializeObject(rSet.value);

            await db.StringSetAsync(String.Concat(prependString, "_", rSet.key), _value, TimeSpan.FromSeconds(rSet.expiry.Value));

            return rSet;
        }

        public async Task<ICache.RedisRead> CacheRead(ICache.RedisRead rGet)
        {
            string redisConnection = _config["redisConnectionString"];
            string prependString = _config["redisPrependString"] ?? "LOCAL";

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnection);
            IDatabase db = redis.GetDatabase();

            string _value = await db.StringGetAsync(String.Concat(prependString, "_", rGet.key));

            rGet.hasValue = !String.IsNullOrWhiteSpace(_value);

            if (rGet.hasValue)
            {
                rGet.value = JToken.Parse(_value);
            }

            return rGet;
        }
    }
}
