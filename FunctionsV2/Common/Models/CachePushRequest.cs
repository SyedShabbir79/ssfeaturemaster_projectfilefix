using System.Collections.Generic;

namespace FunctionsV2.Common.Models
{
    public class CachPushRequest
    {
        public string Schema { get; set; }

        public string PartitionKey { get; set; }

        public Dictionary<string, object> NewObject { get; set; }

        public string FilterQuery { get; set; }

        public bool ValidateSchema { get; set; }
    }  
}
