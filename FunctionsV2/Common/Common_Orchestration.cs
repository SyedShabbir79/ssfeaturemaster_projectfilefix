using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionsV2.Common
{
    public static class Common_Orchestration
    {
        [FunctionName("Orchestration_HttpStart")]
        [OpenApiOperation]
        [OpenApiParameter("function", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiRequestBody("application/json", typeof(object))]
        public static async Task<HttpResponseMessage> HttpStart([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/orchestration/start")] HttpRequestMessage req, HttpRequest req2, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            object eventData = await req.Content.ReadAsAsync<object>();
            string instanceId = await starter.StartNewAsync(req2.Query["function"], eventData);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}