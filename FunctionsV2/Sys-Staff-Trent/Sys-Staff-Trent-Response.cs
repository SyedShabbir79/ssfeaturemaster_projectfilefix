using Azure;
using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Sys_Staff_Trent.Models;
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
    public class Sys_Staff_Trent_Response
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly IEventGrid _eventGrid;
        private readonly IConfiguration _config;
        public Sys_Staff_Trent_Response(ITableStorageService tableStorageService,
            IEventGrid eventGrid,
            IConfiguration config)
        {
            _tableStorageService = tableStorageService;
            _eventGrid = eventGrid;
            _config = config;
        }

        [FunctionName("Sys_Staff_Trent_Response")]
        [OpenApiOperation]
        [OpenApiParameter("TransactionReference", Required = true, Type = typeof(string))]
        [OpenApiParameter("IsSuccessful", Required = true, Type = typeof(string))]
        [OpenApiRequestBody("application/json", typeof(TrentApiResponse))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Starting Sys_Staff_Trent_Response");

                // Parse Trent response
                TrentApiResponse apiRequest = await BodyReader.Read<TrentApiResponse>(req.Body);
                string queueId = apiRequest.Notification.TransactionReference;
                bool dcSuccess = apiRequest.Notification.IsSuccessful.Equals(
                    "T", StringComparison.Ordinal);

                // Get required environment variables
                string TableName = _config["trentApiLogTable"];
                string PartitionName = _config["trentApiLogPartition"];
                string EventSubject = _config["trentApiEventSubject"];
                string BlobContainer = _config["trentApiBlobContainer"];

                // Get existing request data from table storage
                var resultSet = await _tableStorageService
                   .QueryTableStorageByFilter<TrentApiRespTableEnity>(
                   TableName, $"PartitionKey eq '{PartitionName}' and RowKey eq '{queueId}'");

                TrentApiRespTableEnity existingEntity;

                if (resultSet.Count > 0)
                {
                    existingEntity = resultSet[0];
                }
                else
                {
                    return new OkObjectResult(new BaseFunctionResponse());
                }

                // Initialise table storage update request
                var tsRequest = new TSRequest
                {
                    Schema = TableName,
                    PartitionKey = PartitionName,
                    RowKey = queueId
                };

                // Initialise table storage request content
                JsonElement success = JsonSerializer.SerializeToElement(!dcSuccess ? 0 : 1);
                JsonElement errorText;

                if (null != apiRequest.Notification.Errors)
                {
                    errorText = JsonSerializer.SerializeToElement(
                        apiRequest.Notification.Errors.Error.ErrorDescription);
                }
                else
                {
                    errorText = JsonSerializer.SerializeToElement(
                        apiRequest.Notification.Errors);
                }

                Dictionary<string, JsonElement> tsProperties = new()
                {
                    { "Success", success },
                    { "Error", errorText }
                };

                // Update table storage
                log.LogInformation($"Sys_Staff_Trent_Response: Updating entity in table storage");
                AddOrUpdateTSResponse tsResult = _tableStorageService.UpdateEntity(
                    tsRequest, tsProperties).Result;

                // Publish errors to Event Grid
                if (!dcSuccess)
                {
                    log.LogInformation($"Sys_Staff_Trent_Response: Publishing Error to EGD");

                    // Initialise Dictionary of event data
                    var data = new Dictionary<string, object>()
                    {
                        { "queueId", queueId },
                        { "error", errorText },
                        { "payloadPath", $"{BlobContainer}/{queueId}.json" },
                    };

                    if (existingEntity != null)
                    {
                        data["workflowId"] = existingEntity.WorkflowId;
                        data["personRefNo"] = existingEntity.PersonRefNo;
                        data["operation"] = existingEntity.Operation;
                    }
                    else
                    {
                        data["requestDataMissing"] = true;
                    }

                    // Publish to event grid
                    Response egdResult = PublishEvent(
                        TableName, PartitionName, EventSubject, queueId, data).Result;

                    if (egdResult != null || egdResult.IsError)
                    {
                        log.LogInformation($"Sys_Staff_Trent_Response: Failed to publish to EGD");
                    }
                }
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

        private async Task<Response> PublishEvent(
            string tableName,
            string partitionName,
            string eventSubject,
            string queueId,
            Dictionary<string, object> data)
        {

            // Initialise Event Grid request
            var egRequest = new EGRequest
            {
                Topic = tableName,
                EventType = partitionName,
                Subject = eventSubject,
                Id = queueId,
                Data = data
            };

            // Publish event
            Response egdResult = await _eventGrid.PublishToEventGrid(egRequest);

            return egdResult;
        }
    }
}