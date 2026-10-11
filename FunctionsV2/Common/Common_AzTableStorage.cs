using Azure;
using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FunctionsV2.Common
{
    internal class Common_AzTableStorage
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly IConfiguration _config;

        public Common_AzTableStorage(ITableStorageService tableStorageService,
            IBlobStorageService blobStorageService,
            IConfiguration config)
        {
            _tableStorageService = tableStorageService;
            _config = config;
        }

        public class AzTableRowRequest: TSRequest
        { 
            required public Dictionary<string, object> AdditionalColumns {  get; set; }
        }


        [FunctionName("Common_AzTableRowAdd")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(AzTableRowRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, 
            ILogger log)
        {
            try
            {
                log.LogInformation($"Starting Common_AzTableRowAdd");

                // Get request body
                if (req.Body == null || req.ContentLength == 0)
                    throw new ArgumentException("Request body cannot be null", nameof(req));
                AzTableRowRequest rowRequest = await BodyReader.Read<AzTableRowRequest>(req.Body);

                // Check table
                if (String.IsNullOrWhiteSpace(rowRequest.Schema))
                    throw new ArgumentException("QueueId must not be null", nameof(rowRequest.Schema));
                
                // Check partition key
                if (String.IsNullOrWhiteSpace(rowRequest.PartitionKey))
                    throw new ArgumentException("QueueId must not be null", nameof(rowRequest.PartitionKey));

                // Check row key
                if (String.IsNullOrWhiteSpace(rowRequest.RowKey))
                    throw new ArgumentException("Payload must not be null", nameof(rowRequest.RowKey));

                log.LogInformation($"Common_AzTableRowAdd: Processing request {rowRequest.RowKey}");

                // Create a Dictionary<string, JsonElement> from the RequestDetails element in the request 
                string json = JsonSerializer.Serialize(rowRequest.AdditionalColumns);
                Dictionary<string, JsonElement> additionalColumns = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                // Insert a new row in Table Storage
                log.LogInformation($"Common_AzTableRowAdd: Writing to Table Storage");
                AddOrUpdateTSResponse tsResult = _tableStorageService.InsertEntity(
                    new TSRequest()
                    {
                        Schema = rowRequest.Schema,
                        PartitionKey = rowRequest.PartitionKey,
                        RowKey = rowRequest.RowKey,
                    }, 
                    additionalColumns).Result;


                return new OkObjectResult(
                    new BaseFunctionResponse
                    {
                        Response = tsResult
                    }
                );
            }
            catch (Exception ex)
            {
                log.LogError($"Common_AzTableRowAdd Error - {ex.Message}");
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
