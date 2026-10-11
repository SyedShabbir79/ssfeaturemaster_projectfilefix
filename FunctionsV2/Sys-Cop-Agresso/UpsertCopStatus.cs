using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Sys_Cop_Agresso;
using FunctionsV2.Sys_Cop_Agresso.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace FunctionV2.Sys_Cop_Agresso
{
    public class UpsertCopStatus
    {
        private readonly IUpsertCopStatusService _upsertCopStatusService;

        public UpsertCopStatus(IUpsertCopStatusService upsertCopStatusService)
        {
            _upsertCopStatusService = upsertCopStatusService;
        }

        [OpenApiOperation]        
        [OpenApiRequestBody("application/json", typeof(CoPRequest))]

        [FunctionName("Agresso_Upsert_CopStatus")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "Post", Route = null)] HttpRequest req, 
            ILogger log)
        {
            try
            {              
                log.LogInformation($"Starting Agresso_Upsert_CopStatus");
                CoPRequest httpReq = await BodyReader.Read<CoPRequest>(req.Body);
                BaseFunctionResponse result = _upsertCopStatusService.UpsertCopStatus(httpReq);

                if (!result.Error.IsNullOrDefault())
                    return new BadRequestObjectResult(result);

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError($"Error - {ex.Message}");
                return new BadRequestObjectResult(new BaseFunctionResponse
                {
                    Error = new BaseFunctionResponseError
                    {
                        Code = $"{((int)ErrorCodes.Unknown)}",
                        Message = ex.Message
                    }
                });
            }
        }
    }
}