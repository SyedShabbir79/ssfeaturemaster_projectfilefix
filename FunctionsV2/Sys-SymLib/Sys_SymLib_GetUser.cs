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
    public class Sys_SymLib_GetUser
    {
        private readonly ISymLibService _symLibService;

        public Sys_SymLib_GetUser(ISymLibService symLibService)
        {
            _symLibService = symLibService;
        }

        [FunctionName("Sys_SymLib_GetUser")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(GetRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(SIRSIDynix.Circulation.User))]
        public async Task<IActionResult> GetUser([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            try
            {
                GetRequest request = await BodyReader.Read<GetRequest>(req.Body);
                BaseFunctionResponse response = _symLibService.GetUserFromSymLib(request);

                if (!response.Error.IsNullOrDefault())
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
