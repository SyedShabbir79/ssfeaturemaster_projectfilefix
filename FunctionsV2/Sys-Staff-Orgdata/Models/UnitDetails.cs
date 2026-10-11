using Newtonsoft.Json;

namespace UoR.Sys.Staff.OrgData.Models
{
    public class UnitDetails
    {
        [JsonProperty("endDate")]
        public string EndDate { get; set; }
        [JsonProperty("parent_id")]
        public string ParentId { get; set; }
        [JsonProperty("startDate")]
        public string StartDate { get; set; }
        [JsonProperty("unit_name")]
        public string UnitName { get; set; }
        [JsonProperty("unit_status")]
        public string UnitStatus { get; set; }
    }
}