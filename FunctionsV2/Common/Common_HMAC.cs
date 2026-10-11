using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Security.Cryptography;
using System.Net;

namespace FunctionsV2.Common
{
    public static class Common_HMAC
    {


        public class hmacRequest
        {
            public string key { get; set; }
            public string plainText { get; set; }
        }

        private static byte[] stringToBytes(string plainText)
        {
            return System.Text.Encoding.UTF8.GetBytes(plainText);
        }

        public static string HashHMAC(string key, string plainText)
        {
            byte[] _key = stringToBytes(key);
            byte[] _plainText = stringToBytes(plainText);

            var hash = new HMACSHA256(_key);
            byte[] hashResult = hash.ComputeHash(_plainText);
            return System.Convert.ToBase64String(hashResult);
        }



        [FunctionName("Common_HMAC_Hash")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(hmacRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            hmacRequest request = await System.Text.Json.JsonSerializer.DeserializeAsync<hmacRequest>(req.Body);

            string hmacResult = HashHMAC(request.key, request.plainText);
            return new OkObjectResult(hmacResult);
        }
    }
}
