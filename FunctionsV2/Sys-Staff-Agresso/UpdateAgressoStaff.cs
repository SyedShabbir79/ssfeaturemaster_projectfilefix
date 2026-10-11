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

namespace FunctionV2.Sys_Staff_Agresso
{
    public partial class UpdateAgressoStaff
    {

        private readonly IConfiguration _config;
        private readonly IKeyVault _kv;

        public UpdateAgressoStaff(IConfiguration config, IKeyVault kv)
        {
            this._config = config;
            this._kv = kv;
        }

        [FunctionName("Agresso_Upsert_Staff")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(Employee))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "agresso/updatestaff")] HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("Start AgressoUpdateStaff Function call");
            
            try
            {
                Employee employee = await req.Content.ReadAsAsync<Employee>();

                var w = new Worker(_config, _kv, employee);
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