using System;
using System.Collections.Generic;

namespace FunctionsV2.Sys_Staff_Trent_Api_Log.Models
{
    public class TrentApiRequestDetails
    {
        public string QueueId { get; set; }
        public Dictionary<string, object> RequestDetails { get; set; }
        public string Payload { get; set; }
     }
}