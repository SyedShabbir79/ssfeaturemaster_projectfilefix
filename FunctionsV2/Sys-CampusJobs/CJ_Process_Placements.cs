using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FunctionsV2.Sys_CampusJobs.Models;
using FunctionsV2.Helpers;
using FunctionsV2.Sys_CampusJobs.Services.Interfaces;
using FunctionsV2.Helpers.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;

namespace FunctionsV2.Sys_CampusJobs
{
    public class CJ_Process_Placements 
    {
        private readonly IProcessPlacementsService _processPlacementsService;

        public CJ_Process_Placements(IProcessPlacementsService processPlacementsService)
        {
            _processPlacementsService = processPlacementsService;
        }

        [OpenApiOperation]
        [OpenApiParameter("LastRunTime", Required = true, Type = typeof(DateTime))]

        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        [FunctionName("CJ_Process_Placements")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Starting CJ_Process_Placements");
                ProcessPlacementsRequest httpReq = await BodyReader.Read<ProcessPlacementsRequest>(req.Body);
                BaseFunctionResponse getPlacements = _processPlacementsService.CampusJobsProcessPlacements(httpReq.LastRunTime);

                if (!getPlacements.Error.IsNullOrDefault())
                    return new BadRequestObjectResult(getPlacements);

                return new OkObjectResult(getPlacements);
            }
            catch (Exception ex)
            {
                log.LogError($"Error - {ex.Message}");
                return new BadRequestObjectResult(ex);
            }
        }
    }
}
