using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FunctionsV2.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using FunctionsV2.Helpers.Models;

namespace FunctionsV2.Common
{
    public class Common_GetOAuthToken
    {
        private readonly IOAuthToken _token;

        public Common_GetOAuthToken(ICache cache, IOAuthToken token)
        {
            this._token = token;
        }

        [FunctionName("Common_GetOAuthToken")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(IOAuthToken.Token))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        public async Task<IActionResult> CacheRead([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("Received request for azure function Common_GetOAuthToken");

            // Read the request body
            log.LogInformation("Common_GetOAuthToken - Read the request body");
            byte[] bodyArray = new byte[req.Body.Length];
            await req.Body.ReadAsync(bodyArray, 0, bodyArray.Length);

            using (Stream stream = new MemoryStream(bodyArray))
            {
                JToken token = null;
                // Retrieve the request body as string
                log.LogInformation("Common_GetOAuthToken - Retrieve the request body as string");
                string requestBody = await new StreamReader(stream).ReadToEndAsync();
                if (requestBody != null)
                {
                    try
                    {
                        // Deserialize the content into IOAuthToken.Token object
                        log.LogInformation("Common_GetOAuthToken - Deserialize the content into IOAuthToken.Token object");
                        IOAuthToken.Token rGet = Newtonsoft.Json.JsonConvert.DeserializeObject<IOAuthToken.Token>(requestBody);

                        // Check if the RedisRead json token is set
                        log.LogInformation("Common_GetOAuthToken - Check if the RedisRead json token is set");
                        if (rGet != null && rGet.RedisRead != null)
                        {
                            // Retrieve the token from the IOAuthToken Service
                            log.LogInformation("Common_GetOAuthToken - Retrieve the token from the IOAuthToken Service");
                            var redisRead = await _token.CheckIfTokenExistsInCache(rGet, log);

                            if (redisRead != null && redisRead.hasValue)
                            {
                                var tokenExpiry = _token.CheckIfTokenExpired(redisRead, log);

                                if (tokenExpiry)
                                {
                                    token = await _token.GenerateNewToken(rGet, log);
                                }
                                else
                                {
                                    token = redisRead.value;
                                }
                            }
                            else
                            {
                                token = await _token.GenerateNewToken(rGet, log);
                            }
                        }
                        else
                        {
                            // If the RedisRead json token is not set, return a bad object result
                            log.LogError("Common_GetOAuthToken - If the RedisRead json token is not set, return a bad object result");
                            return new BadRequestObjectResult(new BaseFunctionResponse
                            {
                                Error = new BaseFunctionResponseError
                                {
                                    Code = $"{((int)ErrorCodes.BadRequest)}",
                                    Message = "Invalid JSON request received"
                                }
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        // Return exception back to caller
                        log.LogError("Common_GetOAuthToken - Error: " + ex.Message);
                        return new ObjectResult(new BaseFunctionResponse
                        {
                            Error = new BaseFunctionResponseError
                            {
                                Code = $"{((int)ErrorCodes.Unknown)}",
                                Message = ex.Message
                            }
                        })
                        {
                            StatusCode = 500
                        };
                    }
                }

                // Return the token
                log.LogInformation("Common_GetOAuthToken - Return the token");
                return new OkObjectResult(new BaseFunctionResponse
                {
                    Response = new
                    {
                        Token = token
                    }
                });
            }
        }
    }
}
