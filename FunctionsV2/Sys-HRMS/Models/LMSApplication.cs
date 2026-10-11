using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class LMSApplication
    {
        public LMSApplication(string timezone, string impsexclude)
        {
            Timezone = timezone;
            IMPSExclude = impsexclude;
        }
        public string Timezone { get; set; }
        [JsonProperty("impsexclude")]
        public string IMPSExclude { get; set; }
    }
}
