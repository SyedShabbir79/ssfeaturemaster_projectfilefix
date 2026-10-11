using System;
using Azure;
using Azure.Data.Tables;

namespace UoR.Sys.Staff.OrgData.Models
{
    public class OrgDataDump : ITableEntity
    {
        public string Name { get; set; }
        public string RefId { get; set; }
        public string ParentId { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}