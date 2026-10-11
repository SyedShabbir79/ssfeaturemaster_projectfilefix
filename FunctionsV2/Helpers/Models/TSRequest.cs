using System;
using System.Collections.Generic;

namespace FunctionsV2.Helpers.Models
{
    public class TSRequest
    {
        public string Schema { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }    
    }
}
