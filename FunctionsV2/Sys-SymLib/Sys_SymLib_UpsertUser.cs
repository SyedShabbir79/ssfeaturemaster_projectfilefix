using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Sys_SymLib.Models;
using FunctionV2.Sys_SymLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_SymLib
{
    public class Sys_SymLib_UpsertUser
    {
        private readonly ISymLibService _symLibService;

        public Sys_SymLib_UpsertUser(ISymLibService symLibService)
        {
            _symLibService = symLibService;
        }

        [FunctionName("Sys_SymLib_UpsertUser")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(UpsertRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(SIRSIDynix.Circulation.User))]
        public async Task<IActionResult> UpsertUser([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            try
            {              
                UpsertRequest request = await BodyReader.Read<UpsertRequest>(req.Body);
                BaseFunctionResponse response = _symLibService.UpsertUserInSymLib(request);

                if (!response.Error.IsNullOrDefault() && response.Error.Code != $"{((int)ErrorCodes.OK)}")
                    return new BadRequestObjectResult(response);

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }
    }
}
