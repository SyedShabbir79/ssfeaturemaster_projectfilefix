using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net.Http;
using System;
using Microsoft.Extensions.Configuration;
using FunctionsV2.Helpers;
using System.Net;
using System.Linq;
using System.Security.Cryptography;

namespace FunctionsV2.Sys_Payments
{
    public class TransferMateHashCalc
    {
        private readonly ILogger<TransferMateHashCalc> _logger;
        private readonly IKeyVault _kv;

        public TransferMateHashCalc(ILogger<TransferMateHashCalc> log, IKeyVault kv)
        {
            _logger = log;
            this._kv = kv;
        }

        public class TMProperty
        {
            public string key { get; set; }
            public string value { get; set; }
        }


        public static class Base16
        {
            private static readonly char[] encoding;

            static Base16()
            {
                encoding = new char[16]
                {
            '0', '1', '2', '3', '4', '5', '6', '7',
            '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
                };
            }

            public static string Encode(byte[] data)
            {
                char[] text = new char[data.Length * 2];

                for (int i = 0, j = 0; i < data.Length; i++)
                {
                    text[j++] = encoding[data[i] >> 4];
                    text[j++] = encoding[data[i] & 0xf];
                }

                return new string(text);
            }
        }

        private static byte[] stringToBytes(string plainText)
        {
            return System.Text.Encoding.UTF8.GetBytes(plainText);
        }

        [FunctionName("TransferMateHashCalc")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(TMProperty[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The Hash response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "payments/tmhashcalc")] HttpRequestMessage req)
        {
            _logger.LogInformation("HTTP trigger for TransferMateHashCalc.");

            TMProperty[] r = await req.Content.ReadAsAsync<TMProperty[]>();

            var plainData = String.Join(":", r.Where(f => f.key != "hmac_signature" && !String.IsNullOrWhiteSpace(f.value)).OrderBy(f => f.key).Select(f => f.value).ToArray());

            string hmacSecret = await _kv.GetSecret("transferMateHMACSecret");

            byte[] _key = stringToBytes(hmacSecret);
            byte[] _plainData = stringToBytes(plainData);

            var hash = new HMACSHA256(_key);
            byte[] hashResult = hash.ComputeHash(_plainData);
            string hmacHash = Base16.Encode(hashResult);

            return new OkObjectResult(hmacHash);
        }
    }
}

