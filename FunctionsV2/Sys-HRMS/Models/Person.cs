using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class Person
    {
        [JsonProperty("person_id")]
        public string Person_Id { get; set; }
        [JsonProperty("personal_reference")]
        public string Personal_Reference { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("forename1")]
        public string Forename1 { get; set; }
        [JsonProperty("forename2")]
        public string Forename2 { get; set; }
        [JsonProperty("forename3")]
        public string Forename3 { get; set; }
        [JsonProperty("surname")]
        public string Surname { get; set; }
        [JsonProperty("preferred_name")]
        public string Preferred_Name { get; set; }
        [JsonProperty("known_as_surname")]
        public string Known_as_Surname { get; set; }
        [JsonProperty("uni_start_date")]
        public string Uni_Start_Date { get; set; }
        [JsonProperty("uni_leave_date")]
        public string Uni_Leave_Date { get; set; }
        [JsonProperty("employee_start_date")]
        public string Employee_Start_Date { get; set; }
        [JsonProperty("employee_end_date")]
        public string Employee_Leave_Date { get; set; }
        [JsonProperty("extension_date")]
        public string Extension_Date { get; set; }
        [JsonProperty("email_address")]
        public string Email_Address { get; set; }
        [JsonProperty("bank_account")]
        public string Bank_Account { get; set; }
        [JsonProperty("sort_code")]
        public string Sort_Code { get; set; }
        [JsonProperty("roll_number")]
        public string Roll_Number { get; set; }
        [JsonProperty("pay_point")]
        public string Pay_Point { get; set; } = null;
        [JsonProperty("address_line1")]
        public string Address_Line1 { get; set; }
        [JsonProperty("address_line2")]
        public string Address_Line2 { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
        [JsonProperty("county")]
        public string County { get; set; }
        [JsonProperty("post_code")]
        public string Post_Code { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("gender")]
        public string Gender { get; set; }
        [JsonProperty("staff_type")]
        public string Staff_Type { get; set; }
        [JsonProperty("work_internal_phone")]
        public string Work_Internal_Phone { get; set; }
        [JsonProperty("ddi_external_phone")]
        public string Ddi_External_Phone { get; set; } = null;
        [JsonProperty("dpa_module_completed")]
        public bool Dpa_Module_Completed { get; set; }
        [JsonProperty("foi_module_completed")]
        public bool Foi_Module_Completed { get; set; }
        [JsonProperty("infosec_module_completed")]
        public bool Infosec_Module_Completed { get; set; }
        [JsonProperty("under_18")]
        public bool Under_18 { get; set; }
        [JsonProperty("unique_id")]
        public string Unique_Id { get; set; } = null;
        [JsonProperty("hesa_id")]
        public string Hesa_Id { get; set; }
        [JsonProperty("student_id")]
        public string Student_Id { get; set; } = null;
        [JsonProperty("risis_tutor")]
        public bool Risis_Tutor { get; set; }
        [JsonProperty("initials")]
        public string Initials { get; set; }
        [JsonProperty("on_payroll")]
        public bool On_Payroll { get; set; }
        [JsonProperty("release_account")]
        public bool Release_Account { get; set; } = true;
        [JsonProperty("remedy_id")]
        public string Remedy_Id { get; set; } = null;
        [JsonProperty("overseas_staff")]
        public string Overseas_Staff { get; set; }
        [JsonProperty("upn")]
        public string Upn { get; set; }
        [JsonProperty("positions")]
        public Position[] Positions { get; set; }
        [JsonProperty("applications")]
        public Applications Applications { get; set; }
    }

}
