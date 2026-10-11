using System;
using System.Collections.Generic;
using System.Text;

namespace UoR.Sys.LMS.UoRLearn.Models
{
    public class Conditions
    {
        public Condition[] conditions { get; set; }
    }
    public class Condition
    {
        public string name { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "operator")]
        public string _operator { get; set; }

        public string value { get; set; }
    }
}
