using Azure;
using Azure.Storage.Blobs.Models;
using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Sys_Staff_Trent_Api_Log.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FunctionV2.Sys_Staff_Trent
{
    public class Sys_Staff_Trent_Request
    {
        private ITableStorageService _tableStorageService;
        private IBlobStorageService _blobStorageService;
        private readonly IConfiguration _config;

        public Sys_Staff_Trent_Request(
            ITableStorageService tableStorageService,
            IBlobStorageService blobStorageService,
            IConfiguration config)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
            _config = config;
        }

        [FunctionName("Sys_Staff_Trent_Request")]
        [OpenApiOperation]
        [OpenApiParameter("QueueId", Required = true, Type = typeof(string))]
        [OpenApiParameter("RequestDetails", Required = true, Type = typeof(Dictionary<string, object>))]
        [OpenApiParameter("Payload", Required = true, Type = typeof(string))]
        [OpenApiRequestBody("application/json", typeof(TrentApiRequestDetails))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Starting Sys_Staff_Trent_Request");

                // Get request body
                if (req.Body == null || req.ContentLength == 0)
                    throw new ArgumentException("Request body cannot be null", nameof(req));
                TrentApiRequestDetails apiRequest = await BodyReader.Read<TrentApiRequestDetails>(req.Body);
    
                // Get required environment variables
                string TableName = _config["trentApiLogTable"];
                string PartitionName = _config["trentApiLogPartition"];
                string BlobContainer = _config["trentApiBlobContainer"];
                if (TableName == null || PartitionName == null || BlobContainer == null)
                    throw new ArgumentException("One or more enviroment variables is missing or null");

                // Get QueueId
                string queueId = apiRequest.QueueId;
                if (String.IsNullOrWhiteSpace(queueId))
                    throw new ArgumentException("QueueId must not be null", nameof(req));

                // Get request payload
                string payload = apiRequest.Payload;
                if (String.IsNullOrWhiteSpace(payload))
                    throw new ArgumentException("Payload must not be null", nameof(req));

                log.LogInformation($"Sys_Staff_Trent_Request: Processing request {queueId}");

                // Create a Dictionary<string, JsonElement> from the RequestDetails element in the request 
                string json = JsonSerializer.Serialize(apiRequest.RequestDetails);
                Dictionary<string, JsonElement> insertProperties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                // Initialise Table Storage request
                var tsRequest = new TSRequest
                {
                    Schema = TableName,
                    PartitionKey = PartitionName,
                    RowKey = queueId
                };

                // Insert a new row in Table Storage
                log.LogInformation($"Sys_Staff_Trent_Request: Writing to Table Storage");
                AddOrUpdateTSResponse tsResult = _tableStorageService.InsertEntity(
                    tsRequest, insertProperties).Result;

                // Store Payload as a JSON file in Blob Storage
                log.LogInformation($"Sys_Staff_Trent_Request: Writing to Blob Storage");
                Response<BlobContentInfo> blobResult = WriteBlob(
                    BlobContainer, queueId, payload).Result;

                return new OkObjectResult(
                    new BaseFunctionResponse
                    {
                        Response = tsResult
                    }
                );
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

        private async Task<Response<BlobContentInfo>> WriteBlob(string blobContainer, string queueId, string blobPayload)
        {
            // Initialise Blob Storage request
            var blobRequest = new BlobRequest
            {
                Container = blobContainer,
                FileName = queueId,
                Content = blobPayload
            };

            // Store Payload as a JSON file in Blob Storage
            Response<BlobContentInfo> blobResponse = await _blobStorageService.AddJsonEntityToBlobStorage(blobRequest);

            return blobResponse;
        }
    }
}