using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net;
using FunctionsV2.Helpers;
using System.Collections.Generic;
using System.IO.Compression;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading;
using System.IO;
using RestSharp;
using Newtonsoft.Json;

namespace FunctionsV2.Common
{
    public class Common_ZIP
    {
        private readonly IKeyVault keyVault;
        private readonly IConfiguration config;

        public Common_ZIP(IKeyVault keyVault, IConfiguration config)
        {
            this.keyVault = keyVault;
            this.config = config;
        }

        public class Blob2UnZip_Request
        {
            public string blobSASToken { get; set; }
            public string destFolder { get; set; }
            public string domain { get; set; }
            public string username { get; set; }
            public string secretName { get; set; }
            public int loginType { get; set; }
        }

        public class JsonDeserializer
        {
            public List<string> DeserializeToList(string content)
            {
                try
                {
                    // Try deserializing as a JSON array
                    return JsonConvert.DeserializeObject<List<string>>(content) ?? new List<string>();
                }
                catch
                {
                    // Fallback for single string
                    return new List<string> { content };
                }
            }
        }

#if !DEBUG 

        [FunctionName("Common_ZIP_Blob2UnZip")]
        public async Task<List<string>> Blob2UnZip([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            Blob2UnZip_Request req = context.GetInput<Blob2UnZip_Request>();
            return await context.CallActivityAsync<List<string>>("Blob2UnZip_Worker", req);
        }

        [FunctionName("Blob2UnZip_Worker")]
        public async Task<List<string>> Blob2UnZip_Worker([ActivityTrigger] Blob2UnZip_Request req, ILogger log)
        {
            List<string> extractedFiles = new List<string>();

            string opdgProxyHostname = config["opdgProxyHostname"];
            log.LogInformation($"opdgProxy: {opdgProxyHostname}");

            //var rc = new RestSharp.RestClient($"http://{opdgProxyHostname}:62001");
            //var r = new RestSharp.RestRequest(@$"/api/unzip/Blob2Unzip");
            //r.AddJsonBody(req);
            ////var x = await rc.ExecutePostAsync<List<string>>(r);
            //var x = await rc.ExecuteAsync<string>(r, Method.Post);

            //log.LogInformation($"HTTP Response From API Call: {x.StatusCode }, Content: {x.Content}");

            //if (x.ErrorException != null)
            //{
            //    log.LogError($"HTTP ErrorException API Call");
            //    throw new Exception(x.Content, x.ErrorException);
                
            //  }

            //if (x.StatusCode == HttpStatusCode.OK)
            //{
                
            //    var response = new List<string> { x.Data };
            //    log.LogInformation($"Call Response Data: {response}");
            //    return response;
            //}
            //else
            //{
            //    log.LogError($"HTTP Code: {x.StatusCode }, Content: {x.Content}");
            //    throw new Exception(x.Content);
            //}

            var rc = new RestClient($"http://{opdgProxyHostname}:62001");
            var r = new RestRequest("/api/unzip/Blob2Unzip", Method.Post);
            r.AddJsonBody(req);

            // Execute request and handle deserialization manually
            var response = await rc.ExecuteAsync(r);

            log.LogInformation($"HTTP Response From API Call: {response.StatusCode }, Content: {response.Content}");

            if (response.ErrorException != null)
            {
                log.LogError($"HTTP ErrorException API Call");
                throw new Exception(response.Content, response.ErrorException);

            }


            if (response.IsSuccessful)
            {
                log.LogInformation($"HTTP Call Successful: {response.StatusCode}, Content: {response.Content}");
                var deserializer = new JsonDeserializer();
                var result = deserializer.DeserializeToList(response.Content);
                return result;
            }
            else
            {
                log.LogError($"HTTP Code: {response.StatusCode}, Content: {response.Content}");
                throw new Exception($"Request failed with status code {response.StatusCode}: {response.Content}");
            }

        }
#endif
    }

}
