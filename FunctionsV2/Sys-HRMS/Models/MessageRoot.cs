using Newtonsoft.Json;
using static FunctionsV2.Sys_HRMS.Sap_Position;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class MessageRoot
    {
        [JsonProperty("sapPerson")]
        public SapEmployee Person { get; set; }

        [JsonProperty("council_president")]
        public string CouncilPresident { get; set; }

        [JsonProperty("dtt")]
        public MainPositionDTT MainPositionDTT { get; set; }

        [JsonProperty("HRMSStaffPush")]
        public HrmsStaffPush HrmsStaffPush { get; set; }
    }
}
