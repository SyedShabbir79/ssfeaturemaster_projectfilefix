using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Cronos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using System.Net.Http;

namespace FunctionsV2.Common
{
    public class Common_Cache
    {
        private readonly ICache _cache;

        public Common_Cache(ICache _cache)
        {
            this._cache = _cache;
        }





        [FunctionName("Common_CacheWrite")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(ICache.RedisWrite))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ICache.RedisWrite))]
        public async Task<IActionResult> CacheWrite([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequestMessage req, ILogger log, ExecutionContext context)
        {

            ICache.RedisWrite rw = await req.Content.ReadAsAsync<ICache.RedisWrite>();

            rw = await _cache.CacheWrite(rw);
            return new OkObjectResult(rw);



        }







        [FunctionName("Common_CacheRead")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(ICache.RedisRead))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ICache.RedisRead))]
        public async Task<IActionResult> CacheRead([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            byte[] bodyArray = new byte[req.Body.Length];
            await req.Body.ReadAsync(bodyArray, 0, bodyArray.Length);

            using (Stream stream = new MemoryStream(bodyArray))
            {
                string requestBody = await new StreamReader(stream).ReadToEndAsync();
                ICache.RedisRead rGet = Newtonsoft.Json.JsonConvert.DeserializeObject<ICache.RedisRead>(requestBody);
                rGet = await _cache.CacheRead(rGet);
                return new OkObjectResult(rGet);
            }
        }

    }
}
