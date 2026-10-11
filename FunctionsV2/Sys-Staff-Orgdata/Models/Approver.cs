using Newtonsoft.Json;

namespace UoR.Sys.Staff.OrgData.Models
{
    public class Approver
    {
        [JsonProperty("staff_id")]
        public string StaffId { get; set; }
        [JsonProperty("unit_id")]
        public string UnitId { get; set; }
        [JsonProperty("username")]
        public string UserName { get; set; }
        public int Level { get; set; }
    }
}