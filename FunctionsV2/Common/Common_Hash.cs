using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace FunctionsV2.Common
{
    public static class Common_Hash
    {
        private static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        [FunctionName("Common_Hash")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            return new OkObjectResult(new { hash = CreateMD5(requestBody) });
        }
    }
}
