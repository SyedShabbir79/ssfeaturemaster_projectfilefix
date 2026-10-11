using FunctionsV2.Sys_StudentPhoto.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_StudentPhoto
{
    public class ResizeHistoricStudentPhotosFunction
    {
        private readonly IStudentPhotoResizeService _service;
        private readonly IConfiguration _config;


        public ResizeHistoricStudentPhotosFunction(IStudentPhotoResizeService service, IConfiguration config)
        {
            _service = service;
            _config = config;
        }

        [FunctionName("Sys_Resize_Historic_Student_Photos")]
        public async Task Run([TimerTrigger("%HistoricStudentPhotosSchedule%")] TimerInfo timer, ILogger log)
        {
            if (!_config.GetValue<bool>("EnableHistoricResize"))
            {
                log.LogInformation("Historic resize function is disabled via configuration.");
                return;
            }

            log.LogInformation("Historic resize function triggered at: {Time}", DateTime.UtcNow);
            var batchSize = _config.GetValue<int?>("HistoricStudentPhotosBatchSize") ?? 200;
            var rows = await _service.GetUnresizedBatchAsync(batchSize);

            int ok = 0, fail = 0;
            foreach (var row in rows)
            {
                try
                {
                    var fileName = string.IsNullOrWhiteSpace(row.PhotoFileName) ? $"{row.StuCode}.jpg" : row.PhotoFileName;
                    await _service.ResizeAndUpsertFromDbPhotoAsync(row.StuCode, row.Photo, fileName);
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    log.LogWarning(ex, "Historic resize failed for StuCode={StuCode}", row.StuCode);
                }
            }

            log.LogInformation("Historic resize complete. Success={Ok}, Failed={Fail}", ok, fail);
        }
    }
}