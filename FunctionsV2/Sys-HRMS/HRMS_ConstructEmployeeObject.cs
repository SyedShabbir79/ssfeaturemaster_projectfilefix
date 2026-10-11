using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FunctionsV2.Helpers;
using FunctionsV2.Sys_HRMS.Services.Interfaces;
using FunctionsV2.Sys_HRMS.Models;
using FunctionsV2.Helpers.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;

namespace FunctionsV2.Sys_HRMS
{
    public class HRMS_ConstructEmployeeObject
    {
        private readonly IConstructEmployeeObjectService _constructEmployeeObjectService;

        public HRMS_ConstructEmployeeObject(IConstructEmployeeObjectService constructEmployeeObjectService)
        {
            _constructEmployeeObjectService = constructEmployeeObjectService;
        }

        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(ConstrictEmployeeObjectRequest))]

        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        [FunctionName("HRMS_ConstructEmployeeObject")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Starting HRMS_ConstructEmployeeObject");
                ConstrictEmployeeObjectRequest httpReq = await BodyReader.Read<ConstrictEmployeeObjectRequest>(req.Body);
                BaseFunctionResponse employeeObject = _constructEmployeeObjectService.ConstructEmployeeObject(httpReq);

                if (!employeeObject.Error.IsNullOrDefault())
                    return new BadRequestObjectResult(employeeObject);

                return new OkObjectResult(employeeObject);
            }
            catch (Exception ex)
            {
                log.LogError($"Error - {ex.Message}");
                return new BadRequestObjectResult(ex);
            }
        }
    }
}

