using Newtonsoft.Json;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class SapEmployee
    {
        [JsonProperty("personal_reference")]
        public string Sap_Personal_Reference { get; set; }
        [JsonProperty("title")]
        public string Sap_Title { get; set; }
        [JsonProperty("surname")]
        public string Sap_Surname { get; set; }
        [JsonProperty("forename1")]
        public string Sap_Forename1 { get; set; }
        [JsonProperty("forename2")]
        public string Sap_Forename2 { get; set; }
        [JsonProperty("forename3")]
        public string Sap_Forename3 { get; set; }
        [JsonProperty("preferred_name")]
        public string Sap_Preferred_Name { get; set; }
        [JsonProperty("known_as_surname")]
        public string Sap_Known_as_Surname { get; set; }
        [JsonProperty("uni_start_date")]
        public string Sap_Uni_Start_Date { get; set; }
        [JsonProperty("uni_leave_date")]
        public string Sap_Uni_Leave_Date { get; set; }
        [JsonProperty("email_address")]
        public string Sap_Email_Address { get; set; }
        [JsonProperty("bank_account")]
        public string Sap_Bank_Account { get; set; }
        [JsonProperty("sort_code")]
        public string Sap_Sort_Code { get; set; }
        [JsonProperty("roll_number")]
        public string Sap_Roll_Number { get; set; }
        [JsonProperty("address_line1")]
        public string Sap_Address_Line1 { get; set; }
        [JsonProperty("address_line2")]
        public string Sap_Address_Line2 { get; set; }
        [JsonProperty("city")]
        public string Sap_City { get; set; }
        [JsonProperty("county")]
        public string Sap_County { get; set; }
        [JsonProperty("post_code")]
        public string Sap_Post_Code { get; set; }
        [JsonProperty("country")]
        public string Sap_Country { get; set; }
        [JsonProperty("gender")]
        public string Sap_Gender { get; set; }
        [JsonProperty("staff_type")]
        public string Sap_Staff_Type { get; set; }
        [JsonProperty("work_internal_phone")]
        public string Sap_Work_Internal_Phone { get; set; }
        [JsonProperty("dpa_module_completed")]
        public bool Sap_Dpa_Module_Completed { get; set; }
        [JsonProperty("foi_module_completed")]
        public bool Sap_Foi_Module_Completed { get; set; }
        [JsonProperty("infosec_module_completed")]
        public bool Sap_Infosec_Module_Completed { get; set; }
        [JsonProperty("under_18")]
        public bool Sap_Under_18 { get; set; }
        [JsonProperty("hesa_id")]
        public string Sap_Hesa_Id { get; set; }
        [JsonProperty("risis_tutor")]
        public bool Sap_Risis_Tutor { get; set; }
        [JsonProperty("initials")]
        public string Sap_Initials { get; set; }
        [JsonProperty("on_payroll")]
        public bool Sap_On_Payroll { get; set; }
        [JsonProperty("location")]
        public string Sap_Location { get; set; }
        [JsonProperty("upn")]
        public string Sap_Upn { get; set; }
        [JsonProperty("positions")]
        public Sap_Position[] Sap_Positions { get; set; }
        [JsonProperty("applications")]
        public Applications Applications { get; set; }
    }

}
