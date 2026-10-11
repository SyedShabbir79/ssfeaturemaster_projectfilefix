using Newtonsoft.Json;

namespace UoR.Sys.Staff.OrgData.Models
{
    public class OrgData
    {
        [JsonProperty("unit_id")]
        public string UnitId { get; set; }
        [JsonProperty("unit_type")]
        public string UnitType { get; set; }
        [JsonProperty("before")]
        public UnitDetails Before { get; set; }
        [JsonProperty("after")]
        public UnitDetails After { get; set; }
    }
}
