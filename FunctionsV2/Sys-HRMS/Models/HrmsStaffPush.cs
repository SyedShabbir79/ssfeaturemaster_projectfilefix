using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class HrmsStaffPush
    {
        [JsonProperty("id")]
        public int HRMS_Id { get; set; }
        [JsonProperty("staffid")]
        public string HRMS_StaffId { get; set; }
        [JsonProperty("changedetected")]
        public DateTime HRMS_ChangeDetected { get; set; }
        [JsonProperty("uniqueid")]
        public string HRMS_UniqueId { get; set; }
        [JsonProperty("username")]
        public string HRMS_Username { get; set; }
        [JsonProperty("account_status")]
        public string HRMS_Account_Status { get; set; }
        [JsonProperty("emailaddress")]
        public string HRMS_EmailAddress { get; set; }
        [JsonProperty("preferred_name")]
        public string HRMS_Preferred_Name { get; set; }
        [JsonProperty("known_as_surname")]
        public string HRMS_Known_As_Surname { get; set; }
        [JsonProperty("studentid")]
        public string Studentid { get; set; }
        [JsonProperty("extensiondate")]
        public string ExtensionDate { get; set; }
        [JsonProperty("ad_upn")]
        public string HRMS_Ad_Upn { get; set; }
        [JsonProperty("user_type")]
        public string HRMS_User_Type { get; set; }
        [JsonProperty("changeType")]
        public string HRMS_ChangeType { get; set; }
        [JsonProperty("rv")]
        public string HRMS_Rv { get; set; }
    }
}
