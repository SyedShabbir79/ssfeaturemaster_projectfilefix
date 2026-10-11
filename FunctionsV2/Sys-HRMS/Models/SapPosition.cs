using Newtonsoft.Json;
using static FunctionsV2.Sys_HRMS.Sap_Position;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class SapPosition
    {
        [JsonProperty("occupancy_reference")]
        public string Sap_Occupancy_Reference { get; set; }

        [JsonProperty("position_reference")]
        public string Sap_Position_Reference { get; set; }

        [JsonProperty("employee_group")]
        public string Sap_Employee_Group { get; set; }

        [JsonProperty("employee_subgroup")]
        public string Sap_Employee_Subgroup { get; set; }

        [JsonProperty("pay_grade")]
        public string Sap_Pay_Grade { get; set; }

        [JsonProperty("faculty")]
        public string Sap_Faculty { get; set; }

        [JsonProperty("school")]
        public string Sap_School { get; set; }

        [JsonProperty("department")]
        public string Sap_Department { get; set; }

        [JsonProperty("base_unit")]
        public string Sap_Base_Unit { get; set; }

        [JsonProperty("location")]
        public string Sap_Location { get; set; }

        [JsonProperty("pos_occ_start")]
        public string Sap_Pos_Occ_Start { get; set; }

        [JsonProperty("pos_occ_end")]
        public string Sap_Pos_Occ_End { get; set; }

        [JsonProperty("main_position")]
        public string Sap_Main_Position { get; set; }

        [JsonProperty("manager_ref_no")]
        public string Sap_Manager_Ref_No { get; set; }

        [JsonProperty("position_title")]
        public string Sap_Position_Title { get; set; }

        [JsonProperty("position_status")]
        public string Sap_Position_Status { get; set; }

        [JsonProperty("position_unit_id")]
        public string Sap_Position_Unit_Id { get; set; }

        [JsonProperty("position_unit")]
        public SapPositionUnit Sap_Position_Unit { get; set; }
    }
}
