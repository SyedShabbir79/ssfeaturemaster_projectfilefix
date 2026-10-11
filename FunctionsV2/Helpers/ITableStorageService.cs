using Azure;
using Azure.Data.Tables;
using FunctionsV2.Common.Models;
using FunctionsV2.Helpers.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ITableEntity = Azure.Data.Tables.ITableEntity;
using TableEntity = Azure.Data.Tables.TableEntity;

namespace FunctionsV2.Helpers
{
    //Interface
    public interface ITableStorageService
    {
        Task<List<T>> QueryTableStorageByFilter<T>(string tableName, string filterQuery) where T : class, ITableEntity, new();

        Task<AddOrUpdateTSResponse> UpsertEntity(CachPushRequest newObject, Dictionary<string, JsonElement> properties);

        Task<AddOrUpdateTSResponse> InsertEntity(TSRequest newObject, Dictionary<string, JsonElement> properties);

        Task<AddOrUpdateTSResponse> UpdateEntity(TSRequest newObject, Dictionary<string, JsonElement> properties);

        Task UpsertEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity;
    }

    //Service 
    public class TableStorageService : ITableStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly IConfiguration _config;

        private CloudTable _cloudTable;

        public TableStorageService(IConfiguration config)
        {
            _config = config;
            _tableServiceClient = new TableServiceClient(_config["storageAccountConnectionString"]);
        }


        public async Task<List<T>> QueryTableStorageByFilter<T>(string tableName, string filterQuery) where T : class, ITableEntity, new()
        {
            TableClient tableClient = _tableServiceClient.GetTableClient(tableName);
            List<T> entities = new();

            await foreach (T entity in tableClient.QueryAsync<T>(filterQuery))
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task<AddOrUpdateTSResponse> InsertEntity(TSRequest newObject, Dictionary<string, JsonElement> properties)
        {
            List<string> changedFields = [];

            // Initialise tableClient for specified table
            var tableClient = new TableClient(_config["storageAccountConnectionString"], newObject.Schema);

            // Initialise a table entry
            TableEntity entity = new TableEntity(newObject.PartitionKey, newObject.RowKey);

            // Add properties from request to new table entity, list new keys as changedFields
            foreach (var property in properties)
            {
                entity[property.Key] = property.Value;
                changedFields.Add(property.Key);
            }

            // Insert new entity into table
            await tableClient.AddEntityAsync(entity);

            return new AddOrUpdateTSResponse
            {
                FieldsChanged = changedFields,
                NewRecord = true
            };

        }

        public async Task<AddOrUpdateTSResponse> UpdateEntity(TSRequest newObject, Dictionary<string, JsonElement> properties)
        {
            List<string> changedFields = [];

            // Initialise tableClient for specified table
            var tableClient = new TableClient(_config["storageAccountConnectionString"], newObject.Schema);

            // Retrieve existing entity (row)
            Response<TableEntity> response = await tableClient.GetEntityAsync<TableEntity>(newObject.PartitionKey, newObject.RowKey);
            TableEntity entity = response.Value;

            foreach (var property in properties)
            {
                entity[property.Key] = property.Value;
                changedFields.Add(property.Key);
            }

            // Merge new entity defintion with existing one
            await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

            // Return AddOrUpdateTSResponse
            return new AddOrUpdateTSResponse
            {
                FieldsChanged = changedFields,
                NewRecord = false
            };

        }

        public async Task<AddOrUpdateTSResponse> UpsertEntity(CachPushRequest newObject, Dictionary<string, JsonElement> properties)
        {
            List<string> changedFields = new();

            //Get connected to the table specified by the schema.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_config["storageAccountConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            _cloudTable = tableClient.GetTableReference(newObject.Schema);

            //Convert all properties into EntityProperties of the valid types.
            Dictionary<string, EntityProperty> convertedProperties = ConvertToEntityProperties(properties);

            //Query table and return results.
            TableQuery<DynamicTableEntity> filterQuery = new TableQuery<DynamicTableEntity>().Where(newObject.FilterQuery);
            TableQuerySegment<DynamicTableEntity> existingEntities = await _cloudTable.ExecuteQuerySegmentedAsync(filterQuery, null);

            DynamicTableEntity entity;

            //If there are results from the filterQuery (Update)
            if (existingEntities.Results.Count > 0)
            {
                DynamicTableEntity existing = existingEntities.Results[0];
                string rowKey = existing.RowKey;

                //If the new value and the existing value are not equal, add it to the 'changedFields' variable
                foreach (var keyValuePair in convertedProperties)
                {
                    if (!existing.Properties.ContainsKey(keyValuePair.Key) || !EntityPropertyEquals(existing.Properties[keyValuePair.Key], keyValuePair.Value))
                        changedFields.Add(keyValuePair.Key);
                }

                entity = new DynamicTableEntity(newObject.PartitionKey, rowKey)
                {
                    ETag = "*",
                    Properties = convertedProperties
                };

                //Preform an update / replace on entry
                TableOperation operation = TableOperation.Replace(entity);
                await _cloudTable.ExecuteAsync(operation);

                return new AddOrUpdateTSResponse
                {
                    FieldsChanged = changedFields,
                    NewRecord = false
                };
            }
            //If there are no results from the filterQuery (Insert)
            else
            {
                string newRowKey = Guid.NewGuid().ToString();

                entity = new DynamicTableEntity(newObject.PartitionKey, newRowKey)
                {
                    Properties = convertedProperties
                };

                //Preform an insert to table
                TableOperation operation = TableOperation.Insert(entity);
                await _cloudTable.ExecuteAsync(operation);

                return new AddOrUpdateTSResponse
                {
                    FieldsChanged = ["*"],
                    NewRecord = true
                };
            }
        }

        //Azure Table Storage uses strongly typed storage, this method ensures that each KVP meets the requirements of Azure TS.
        private Dictionary<string, EntityProperty> ConvertToEntityProperties(Dictionary<string, JsonElement> input)
        {
            Dictionary<string, EntityProperty> result = new();

            foreach (var keyValuePair in input)
            {
                object value = keyValuePair.Value;

                if (value == null)
                    result[keyValuePair.Key] = new EntityProperty((string)null);
                else if (value is string s)
                    result[keyValuePair.Key] = new EntityProperty(s);
                else if (value is bool b)
                    result[keyValuePair.Key] = new EntityProperty(b);
                else if (value is int i)
                    result[keyValuePair.Key] = new EntityProperty(i);
                else if (value is long l)
                    result[keyValuePair.Key] = new EntityProperty(l);
                else if (value is double d)
                    result[keyValuePair.Key] = new EntityProperty(d);
                else if (value is DateTime dt)
                {
                    //If there is no timestamp with the date, convert to yyy-MM-dd format in order to get a DateOnly value.
                    if (dt.TimeOfDay.TotalSeconds == 0)
                        result[keyValuePair.Key] = new EntityProperty(dt.ToString("yyyy-MM-dd"));
                    else
                        result[keyValuePair.Key] = new EntityProperty(dt.ToUniversalTime());
                }
                else if (value is Guid g)
                    result[keyValuePair.Key] = new EntityProperty(g);
                else
                    result[keyValuePair.Key] = new EntityProperty(value.ToString());
            }
            return result;

        }

        //Compares to see if two EntityProperties are equal.
        //Used to calculate 'changedFields'
        private bool EntityPropertyEquals(EntityProperty propertyA, EntityProperty propertyB)
        {
            if (propertyA == null || propertyB == null)
                return false;

            if (propertyA.PropertyType != propertyB.PropertyType)
                return false;

            return propertyA.PropertyType switch
            {
                EdmType.String => propertyA.StringValue == propertyB.StringValue,
                EdmType.Int32 => propertyA.Int32Value == propertyB.Int32Value,
                EdmType.Int64 => propertyA.Int64Value == propertyB.Int64Value,
                EdmType.Boolean => propertyA.BooleanValue == propertyB.BooleanValue,
                EdmType.DateTime => propertyA.DateTime == propertyB.DateTime,
                EdmType.Double => propertyA.DoubleValue == propertyB.DoubleValue,
                EdmType.Guid => propertyA.GuidValue == propertyB.GuidValue,
                _ => false
            };
        }

        public async Task UpsertEntityAsync<T>(string tableName, T entity)
           where T : class, ITableEntity
        {
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            await tableClient.UpsertEntityAsync(entity);
        }
    }
}
