using Azure;
using Azure.Data.Tables;
using System;

namespace FunctionsV2.Sys_Staff_Trent.Models
{
    internal class TrentApiRespTableEnity : ITableEntity
    {
        //Default Properties
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }


        //Custom Properties
        public string WorkflowId { get; set; }
        public string Operation { get; set; }
        public string PersonRefNo { get; set; }
        public int Success { get; set; }
        public string Error { get; set; }
    }
}

