using Newtonsoft.Json;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class Position
    {
        [JsonProperty("occupancy_id")]
        public string Occupancy_Id { get; set; }

        [JsonProperty("occupancy_reference")]
        public string Occupancy_Reference { get; set; }

        [JsonProperty("position_reference")]
        public string Position_Reference { get; set; }

        [JsonProperty("pay_category")]
        public string Pay_Category { get; set; }

        [JsonProperty("faculty")]
        public string Faculty { get; set; }

        [JsonProperty("school")]
        public string School { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("base_unit")]
        public string Base_Unit { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("pos_occ_start")]
        public string Pos_Occ_Start { get; set; }

        [JsonProperty("pos_occ_end")]
        public string Pos_Occ_End { get; set; }

        [JsonProperty("main_position")]
        public int Main_Position { get; set; }

        [JsonProperty("manager_ref_no")]
        public string Manager_Ref_No { get; set; }

        [JsonProperty("position_title")]
        public string Position_Title { get; set; }

        [JsonProperty("position_status")]
        public string Position_Status { get; set; }

        [JsonProperty("position_type")]
        public string Position_Type { get; set; }

        [JsonProperty("pos_occ_exp_end")]
        public string Pos_Occ_Exp_End { get; set; }

        [JsonProperty("position_unit_id")]
        public string Position_Unit_Id { get; set; }

        [JsonProperty("position_unit")]
        public PositionUnit Position_Unit { get; set; }
    }
}
