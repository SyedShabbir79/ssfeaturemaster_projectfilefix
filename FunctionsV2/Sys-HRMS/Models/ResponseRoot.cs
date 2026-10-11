using Newtonsoft.Json;
using System.Collections.Generic;
using static FunctionsV2.Sys_HRMS.Sap_Position;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class ResponseRoot
    {
        [JsonProperty("person")]
        public Person Person { get; set; }

        [JsonProperty("HRMSStaffPush")]
        public HrmsStaffPush HrmsStaffPush { get; set; }
        [JsonProperty("errors")]
        public IList<PositionError> Errors { get; set; } = new List<PositionError>();

        [JsonProperty("hasError")]
        public bool HasError { get; set; }

        public void Add(PositionError item)
        {
            this.Errors.Add(item);
        }
    }
}
