using Newtonsoft.Json;
using System.Collections.Generic;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    public class UpdateSapRequest
    {
        [JsonProperty("CustomUser")]
        public TargetConnectCustomUserResponse CustomUser { get; set; }

        [JsonProperty("PlacementRecords")]
        public List<GetPlacementRecord> Placements { get; set; }

        [JsonProperty("SapToken")]
        public string SapToken { get; set; }
    }
}
