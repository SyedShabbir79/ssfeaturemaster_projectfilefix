
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FunctionsV2.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace UoR.AIS.HelperFunctionsV2.Activity
{
    public class PerformCallbackFunction
    {
        private readonly HttpClient _httpClient;

        public PerformCallbackFunction(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [FunctionName("A_PerformCallback")]
        public async Task<object> PerformCallback([ActivityTrigger] IDurableActivityContext executionContext, ILogger log)
        {
            log.LogInformation("Entered callback function");
            //Perform an HTTP post on the provided URL

            var callbackData = executionContext.GetInput<Common_CallbackData>();
            var request = new HttpRequestMessage(HttpMethod.Post, callbackData.CallbackUrl);
            var serializedContent = JsonConvert.SerializeObject(callbackData.Message);
            var buffer = System.Text.Encoding.UTF8.GetBytes(callbackData.Message.ToString());
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = byteContent;
            request.Content.Headers.Add("Status", callbackData.Status);
            await _httpClient.SendAsync(request);
            log.LogInformation("Completed callback function");
            return true;
        }
    }

    

   
}
