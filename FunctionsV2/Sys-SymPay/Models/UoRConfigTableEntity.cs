using Azure;
using Azure.Data.Tables;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FunctionsV2.Sys_SymPay.Models
{
    [ExcludeFromCodeCoverage]
    public class UoRConfigTableEntity : ITableEntity
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public long intValue1 { get; set; }
        public bool boolValue { get; set; }
    }
}
