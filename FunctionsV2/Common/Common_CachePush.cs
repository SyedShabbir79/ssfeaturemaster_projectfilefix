using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Common.Models;
using System.Text.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace FunctionsV2.Common
{
    public class Common_CachePush
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IConfiguration _config;

        public Common_CachePush(ITableStorageService tableStorageService,
            IBlobStorageService blobStorageService,
            IConfiguration config)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
            _config = config;
        }

        [OpenApiOperation]
        [OpenApiParameter("Schema", Required = true, Type = typeof(string))]
        [OpenApiParameter("FilterQuery", Required = true, Type = typeof(string))]
        [OpenApiParameter("PartitionKey", Required = true, Type = typeof(string))]
        [OpenApiParameter("ValidateSchema", Required = true, Type = typeof(bool))]
        [OpenApiParameter("NewObject", Required = true, Type = typeof(Dictionary<string, object>))]

        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        [FunctionName("Common_CachePush")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Starting Common_CachePush");
                CachPushRequest httpReq = await BodyReader.Read<CachPushRequest>(req.Body);
                BaseFunctionResponse result = CompareObjectToTableStorageEntity(httpReq);                

                if (!result.Error.IsNullOrDefault())
                    return new BadRequestObjectResult(result);

                return new OkObjectResult(result);
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

        //--- CachePushService ---
        private BaseFunctionResponse CompareObjectToTableStorageEntity(CachPushRequest newObject)
        {
            try
            {
                //Validate all mandatory data is present
                BaseFunctionResponse dataValidation = DataValidation(newObject);
                if(!dataValidation.IsNullOrDefault())
                    return dataValidation;

                string json  = JsonSerializer.Serialize(newObject.NewObject);
                Dictionary<string, JsonElement> dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                //If ValidateSchema is true, validate the json against the schema in blob storage. 
                if(newObject.ValidateSchema)
                {
                    var (isValid, errors) = ValidateJsonAgainstSchema(newObject.Schema, newObject.NewObject).Result;
                    if (!isValid)
                    {
                        //Group error messages together with count for better readability.
                        return new BaseFunctionResponse
                        {
                            Error = new BaseFunctionResponseError
                            {
                                Code = $"{((int)ErrorCodes.BadRequest)}",
                                Message = $"Validation failed against schema '{newObject.Schema}'. " +
                                    $"{errors.Count} error(s) have been returned: " +
                                    $"{string.Join(", ", errors
                                        .GroupBy(item => item)
                                        .Select(group => $"x{group.Count()} : '{group.Key}'"))}"
                            }
                        };
                    }
                }                

                AddOrUpdateTSResponse result = _tableStorageService.UpsertEntity(newObject, dictionary).Result;

                return new BaseFunctionResponse
                {                    
                    Response = new
                    {
                        result
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseFunctionResponse
                {
                    Error = new BaseFunctionResponseError
                    {
                        Code = $"{((int)ErrorCodes.Unknown)}",
                        Message = $"{ex.Message}"
                    }
                };
            } 
        }

        //Compare object passed in for upsert against the schema stored in blob storage.
        private async Task<(bool isValid, IList<string> errors)> ValidateJsonAgainstSchema(string schemaName, Dictionary<string, object> objectData)
        {
            string schemaContent = await _blobStorageService.RetrieveEntityFromBlobStorage(_config["schemaBlobContainer"], schemaName);
            JSchema schema = JSchema.Parse(schemaContent);

            JObject jsonObject = JObject.FromObject(objectData);
            bool isValid = jsonObject.IsValid(schema, out IList<string> errorMessages);

            return (isValid, errorMessages);
        }

        //Ensure mandatory fields have been provided
        private BaseFunctionResponse DataValidation(CachPushRequest newObject)
        {
            List<string> missingData = new();
            //If there is no PartitionKey or Schema, reject the request 
            if (newObject.PartitionKey.IsNullOrWhiteSpace())
                missingData.Add("PartitionKey");

            if (newObject.Schema.IsNullOrWhiteSpace())
                missingData.Add("Schema");

            if (newObject.NewObject.Count == 0)
                missingData.Add("NewObject");

            if(missingData.Count > 0)
                return ValidateInboundData(missingData);

            return null;
        }

        //Construct error message on nulls / empty values.
        private BaseFunctionResponse ValidateInboundData(List<string> fieldNames)
        {
            return new BaseFunctionResponse
            {
                Error = new BaseFunctionResponseError
                {
                    Code = $"{((int)ErrorCodes.BadRequest)}",
                    Message = $"The following fields are missing: " +
                    $"{string.Join(", ", fieldNames)}"
                }
            };
        }
    }
}