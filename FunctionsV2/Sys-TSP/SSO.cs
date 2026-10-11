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
using System.Net;
using RestSharp;
using System.Collections.Generic;
using System.Linq;

namespace FunctionsV2.Sys_TSP
{
    public static class SSO
    {
        public class ssoRequest
        {
            public Altemailaddress[] altEmailAddresses { get; set; }
            public string emailAddress { get; set; }
            public string tspAccountName { get; set; }
            public string tspEndpoint { get; set; }
            public string tspPassword { get; set; }
            public string tspRedirectBase { get; set; }
            public string tspUsername { get; set; }
        }

        public class Altemailaddress
        {
            public string email { get; set; }
        }

        public class ssoResponse
        {
            public string errorMessage { get; set; }
            public string redirectURL { get; set; }
        }

        private class tspSSOResponse
        {
            public bool success { get; set; }
            public string message { get; set; }
            public TokenObject returnObject { get; set; }
        }

        private class TokenObject
        {
            public string email { get; set; }
            public string token { get; set; }
            public string mobileUrl { get; set; }
            public string desktopUrl { get; set; }
        }

        [FunctionName("TSP-SSO")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(ssoRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ssoResponse))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "tsp/sso")] HttpRequest req, ILogger log)
        {
            try
            {
                List<string> emailAddresses = new List<string>();

                var r = await System.Text.Json.JsonSerializer.DeserializeAsync<ssoRequest>(req.Body);
                RestSharp.RestClient tspClient = new RestSharp.RestClient(r.tspEndpoint);

                emailAddresses.Add(r.emailAddress.ToLower());
                if (r.altEmailAddresses != null && r.altEmailAddresses.Count() > 0)
                {
                    emailAddresses.AddRange(r.altEmailAddresses.Select(s => s.email.ToLower()));
                }
                emailAddresses = emailAddresses.Where(s => s.Length > 0).Distinct().ToList();

                if (emailAddresses.Count() > 0)
                {
                    var tspAuthReq = new RestSharp.RestRequest("/token");
                    tspAuthReq.AddJsonBody(new { accountname = r.tspAccountName, email = r.tspUsername, password = r.tspPassword });
                    var tspAuthResp = await tspClient.ExecuteAsync<string>(tspAuthReq,Method.Post);

                    if (tspAuthResp.IsSuccessful && !string.IsNullOrEmpty(tspAuthResp.Data))
                    {
                        tspClient.AddDefaultHeader("Authorization", tspAuthResp.Data);

                        foreach (string email in emailAddresses)
                        {
                            var tspTokenReq = new RestSharp.RestRequest($"/logintoken?userEmail={email}");

                            try
                            {
                                var tspTokenResp = await tspClient.ExecuteAsync<tspSSOResponse>(tspTokenReq,Method.Get);
                                if (tspTokenResp.IsSuccessful && tspTokenResp.Data.success)
                                {
                                    return new OkObjectResult(new ssoResponse() { errorMessage = "OK", redirectURL = $"{r.tspRedirectBase}?token={tspTokenResp.Data.returnObject.token}" });
                                }
                            }
                            catch (Exception)
                            { }
                        }
                        return new OkObjectResult(new ssoResponse() { errorMessage = "Email address not valid", redirectURL = null });
                    }
                }
                return new BadRequestObjectResult(new ssoResponse() { errorMessage = "Unhandled execution path", redirectURL = null });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new ssoResponse() { errorMessage = ex.Message, redirectURL = null });
            }
        }
    }
}
