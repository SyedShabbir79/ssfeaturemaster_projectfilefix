using Azure;
using Azure.Data.Tables;
using System;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class OrgTableStorageResponse : ITableEntity
    {
        //Default Properties
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }


        //Custom Properties
        public string Name { get; set; }

        public string RefId { get; set; }

        public string ParentId { get; set; }
    }
}
