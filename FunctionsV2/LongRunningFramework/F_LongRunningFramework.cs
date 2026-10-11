using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FunctionsV2.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace FunctionsV2.LongRunningFramework
{
    public static class F_LongRunningFramework
    {
        [FunctionName("F_TaskExecutorStarter")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post" , Route = "task.start")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client, ILogger logger)
        {

            logger.LogInformation("Trigger function F_TaskExecutorStarter processed a request.");

            //Get request body
            var streamReader = new StreamReader(req.Body);
            
            string requestBody = await streamReader.ReadToEndAsync();

            dynamic requestContent = JObject.Parse(requestBody);

            string callBackUrl = requestContent.CallbackUrl;
            JObject inputData = requestContent.RequestBody;
            string functionName = requestContent.FunctionName;

            //Create task info to pass along
            var taskInfo = new Common_TaskInfo(callBackUrl, inputData.ToString(), functionName);

            // Function input comes from the request content.
            string instanceId = await client.StartNewAsync(
                nameof(O_TaskExecutor),taskInfo);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(O_TaskExecutor))]
        public static async Task<object> O_TaskExecutor(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation("Entered O_TaskExecutor function");
            
            var taskInfo = context.GetInput<Common_TaskInfo>();

            log.LogInformation(taskInfo?.CallbackUrl);

            Common_CallbackData callbackData = new Common_CallbackData();
            callbackData.CallbackUrl = taskInfo?.CallbackUrl;
            ActivityResult response;

            try
            {
                //Call first activity function dynamically
                response = await context.CallActivityAsync<ActivityResult>("A_" + taskInfo?.FunctionName, taskInfo?.RequestBody);
                log.LogInformation("Activity function invoked");

                if(response.Status == "Success")
                {
                    callbackData.Message = response.Result;
                    callbackData.Status = response.Status;
                }
                else
                {
                    callbackData.Message = response.ErrorMessage;
                    callbackData.Status = response.Status;
                }
                
            }
            catch(Exception ex)
            {
                callbackData.Message = ex.Message;
                callbackData.Status = "Error";
            }

            //Call next the perform callback activity function
            await context.CallActivityAsync<object>("A_PerformCallback", callbackData);

            log.LogInformation(callbackData?.Message?.ToString());
            log.LogInformation(callbackData?.Status?.ToString());

            log.LogInformation("Completed O_TaskExecutor function");

            return true;
            
        }
        
    }

    public class ActivityResult
    {
        public object Result { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    
}
