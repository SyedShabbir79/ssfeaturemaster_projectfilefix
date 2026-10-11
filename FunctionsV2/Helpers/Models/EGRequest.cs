using System;
using System.Collections.Generic;

namespace FunctionsV2.Helpers.Models
{
    public class EGRequest
    {
        public string Topic { get; set; }
        public string EventType { get; set; }
        public string Subject { get; set; }
        public string Id { get; set; }
        public Dictionary<string,  object> Data { get; set; }
    }
}
