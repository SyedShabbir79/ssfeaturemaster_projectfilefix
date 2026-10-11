using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net.Http;
using System;
using Microsoft.Extensions.Configuration;
using FunctionsV2.Helpers;

namespace FunctionV2.Sys_Staff_Agresso_v2
{
    public partial class UpdateAgressoStaff
    {

        private readonly IConfiguration _config;
				private readonly ISecrets _secrets;

        public UpdateAgressoStaff(IConfiguration config, ISecrets secrets)
        {
            this._config = config;
            this._secrets = secrets;
        }

        [FunctionName("Agresso_Upsert_Staff_v2")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(EmployeeV2))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "agresso/updatestaff/v2")] HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("Start AgressoUpdateStaff");

            try
            {
                EmployeeV2 employee = await req.Content.ReadAsAsync<EmployeeV2>();

                var w = new Worker(_config, _secrets, employee);
                return await w.processUpdate();
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                return new BadRequestObjectResult(ex);
            }
        }
    }
}