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
using FunctionsV2.Helpers;

namespace FunctionsV2.Common
{
    public class Common_KeyVault
    {

        private readonly IKeyVault _kv;


        public Common_KeyVault(IKeyVault kv)
        {
            this._kv = kv;
        }


        [FunctionName("Common_KeyVault_GetSecret")]
        [OpenApiOperation]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string))]
        [OpenApiParameter("secretName", Required = true, Type = typeof(string))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "GET", Route = "common/kv/getSecret")] HttpRequest req, ILogger log)
        {
            string secretName = req.Query["secretName"];

            try
            {
                
                string secret = await _kv.GetSecret(secretName);
                return new OkObjectResult(new { secretName, secret });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("NotFound"))
                    return new NotFoundObjectResult(new { secretName });

                throw;
            }

        }
    }
}
