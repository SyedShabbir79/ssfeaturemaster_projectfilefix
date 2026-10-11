using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FunctionsV2.Helpers;
using FunctionsV2.Sys_CampusJobs.Models;
using FunctionsV2.Sys_CampusJobs.Services.Interfaces;
using FunctionsV2.Helpers.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using FunctionsV2.Sys_HRMS.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace FunctionsV2.Sys_CampusJobs
{
    public class CJ_Update_SAP
    {
        private readonly ISapRequestService _sapRequestService;

        public CJ_Update_SAP(ISapRequestService sapRequestService)
        {
            _sapRequestService = sapRequestService;
        }

        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(SapRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        [FunctionName("CJ_Update_SAP")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Starting CJ_Update_SAP");
                UpdateSapRequest httpReq = await BodyReader.Read<UpdateSapRequest>(req.Body);
                BaseFunctionResponse updateSapResponse = _sapRequestService.ConstructSapPayloadResponse(httpReq);

                if(!updateSapResponse.Error.IsNullOrDefault())
                    return new BadRequestObjectResult(updateSapResponse);

                return new OkObjectResult(updateSapResponse);
            }
            catch (Exception ex)
            {
                log.LogError($"Error - {ex.Message}");
                return new BadRequestObjectResult(ex);
            }
        }
    }
}
