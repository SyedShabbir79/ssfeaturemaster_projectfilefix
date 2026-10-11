using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class MainPositionDTT
    {
        [JsonProperty("faculty_directorate_code")]
        public string FacultyDirectorateCode { get; set; }

        [JsonProperty("its_code")]
        public string ItsCode { get; set; }

        [JsonProperty("risis_code")]
        public string RISISCode { get; set; }

        [JsonProperty("primary_email_domain")]
        public string? PrimaryEmailDomain { get; set; }

        [JsonProperty("secondary_email_domain")]
        public string? SecondaryEmailDomain { get; set; }
    }
}
