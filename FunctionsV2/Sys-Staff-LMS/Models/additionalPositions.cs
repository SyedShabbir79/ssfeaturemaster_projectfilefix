using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace UoR.Sys.LMS.UoRLearn.Models
{
    public class additionalPositions
    {
        public Securitydomain securityDomain { get; set; }
        public CustomValues customValues { get; set; }
        public string id { get; set; }


        public class CustomValues
        {
            [JsonProperty(PropertyName = "custom0")]
            public string Job2Title { get; set; }

            [JsonProperty(PropertyName = "custom1")]
            public string Job2Category { get; set; }

            [JsonProperty(PropertyName = "custom2")]
            public string Job2BaseUnit { get; set; }

            [JsonProperty(PropertyName = "custom3")]
            public string Job2School { get; set; }




            [JsonProperty(PropertyName = "custom4")]
            public string Job3Title { get; set; }

            [JsonProperty(PropertyName = "custom5")]
            public string Job3Category { get; set; }

            [JsonProperty(PropertyName = "custom6")]
            public string Job3BaseUnit { get; set; }

            [JsonProperty(PropertyName = "custom7")]
            public string Job3School { get; set; }



            [JsonProperty(PropertyName = "custom8")]
            public string Job4Title { get; set; }

            [JsonProperty(PropertyName = "custom9")]
            public string Job4Category { get; set; }

            [JsonProperty(PropertyName = "custom10")]
            public string Job4BaseUnit { get; set; }

            [JsonProperty(PropertyName = "custom11")]
            public string Job4School { get; set; }




            [JsonProperty(PropertyName = "custom12")]
            public string Job5Title { get; set; }

            [JsonProperty(PropertyName = "custom13")]
            public string Job5Category { get; set; }

            [JsonProperty(PropertyName = "custom14")]
            public string Job5BaseUnit { get; set; }

            [JsonProperty(PropertyName = "custom15")]
            public string Job5School { get; set; }




            [JsonProperty(PropertyName = "custom16")]
            public string Job6Title { get; set; }

            [JsonProperty(PropertyName = "custom17")]
            public string Job6Category { get; set; }

            [JsonProperty(PropertyName = "custom18")]
            public string Job6BaseUnit { get; set; }

            [JsonProperty(PropertyName = "custom19")]
            public string Job6School { get; set; }

        }
    }
}
