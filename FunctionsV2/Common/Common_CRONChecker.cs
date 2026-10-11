using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Cronos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace FunctionsV2.Common
{
    public static class Common_CRON
    {


        public class CRONCheckRequest
        {
            public string expression { get; set; }
            public bool inclusive { get; set; }
            public DateTime? dateTime { get; set; }
        }

        public class CRONCheckResponse
        {
            public bool result { get; set; }
            public DateTime nextOccurrence { get; set; }
        }





        [FunctionName("Common_CRONCheck")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(CRONCheckRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CRONCheckResponse))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/cron/check")] HttpRequest req, ILogger log)
        {
            try
            {
                CRONCheckRequest request = await System.Text.Json.JsonSerializer.DeserializeAsync<CRONCheckRequest>(req.Body);

                if (request.dateTime.HasValue && !string.IsNullOrWhiteSpace(request.expression))
                {
                    CronExpression expression = CronExpression.Parse(request.expression);
                    DateTime? next = expression.GetNextOccurrence(request.dateTime.Value.ToUniversalTime(), request.inclusive);

                    if (next.HasValue)
                    {
                        return new OkObjectResult(new CRONCheckResponse() { nextOccurrence = next.Value, result = next.Value.Equals(request.dateTime.Value) });
                    }
                }
                return new BadRequestObjectResult(new CRONCheckResponse());
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
            
        }
    }
}
