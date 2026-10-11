using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using FunctionsV2.Helpers;
using FunctionsV2.Sys_HRMS.Services;
using FunctionsV2.Sys_HRMS.Models;
using FunctionsV2.Helpers.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;

namespace FunctionsV2.Sys_HRMS
{
    public class HRMS_Process_SAP_Data
    {
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(MessageRoot))]

        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        [FunctionName("HRMS_Process_SAP_Data")]
        public static async Task<IActionResult> ProcessSapData([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation($"Starting HRMS_Process_SAP_Data");
                MessageRoot httpReq = await BodyReader.Read<MessageRoot>(req.Body);
                BaseFunctionResponse responseRoot = ProcessSapDataService.ConstructEnrichedSapPayload(httpReq);

                if (!responseRoot.Error.IsNullOrDefault())
                    return new BadRequestObjectResult(responseRoot);
               
                return new OkObjectResult(responseRoot);
            }
            catch (Exception ex)
            {
                log.LogError($"Error - {ex.Message}");
                return new BadRequestObjectResult(ex);
            }
        }
    }
}
