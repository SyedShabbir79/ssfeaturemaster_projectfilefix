using System;
using System.Collections.Generic;
using System.Text;

namespace UoR.Sys.LMS.UoRLearn.Models
{
    public class TeamInfo
    {
        public List<LMSBase> directReports { get; set; }
        public List<LMSBase> alternateReports { get; set; }
        public List<LMSBase> alternateManagers { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public LMSBase person { get; set; }
    }
}
