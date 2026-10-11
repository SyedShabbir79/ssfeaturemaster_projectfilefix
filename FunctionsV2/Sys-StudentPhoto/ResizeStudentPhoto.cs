using FunctionsV2.Sys_StudentPhoto.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_StudentPhoto
{
    public class ResizeStudentPhotoFunction
    {
        private readonly IStudentPhotoResizeService _service;

        public ResizeStudentPhotoFunction(IStudentPhotoResizeService service)
        {
            _service = service;
        }

        private sealed class ResizeUpsertRequest
        {
            public string StuCode { get; set; }
            public string Photo { get; set; }
            public string PhotoFileName { get; set; }
        }

        [FunctionName("Sys_Resize_Student_Photo")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(ResizeUpsertRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(object))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            using var sr = new StreamReader(req.Body);
            var payload = JsonSerializer.Deserialize<ResizeUpsertRequest>(
                await sr.ReadToEndAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null || string.IsNullOrWhiteSpace(payload.StuCode) || string.IsNullOrWhiteSpace(payload.Photo))
                return new BadRequestObjectResult("stuCode and photo are required.");

            var result = await _service.ResizeAndUpsertAsync(payload.StuCode, payload.Photo, payload.PhotoFileName);
            return new OkObjectResult(result);
        }
    }
}