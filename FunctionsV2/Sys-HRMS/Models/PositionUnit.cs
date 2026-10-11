using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class PositionUnit
    {
        [JsonProperty("leaf")]
        public string leaf { get; set; }
        [JsonProperty("unit_id_01")]
        public string Unit_Id_01 { get; set; }
        [JsonProperty("unit_nm_01")]
        public string Unit_Nm_01 { get; set; }
        [JsonProperty("unit_id_02")]
        public string Unit_Id_02 { get; set; }
        [JsonProperty("unit_nm_02")]
        public string Unit_Nm_02 { get; set; }
        [JsonProperty("unit_id_03")]
        public string Unit_Id_03 { get; set; }
        [JsonProperty("unit_nm_03")]
        public string Unit_Nm_03 { get; set; }
        [JsonProperty("unit_id_04")]
        public string Unit_Id_04 { get; set; }
        [JsonProperty("unit_nm_04")]
        public string Unit_Nm_04 { get; set; }
        [JsonProperty("unit_id_05")]
        public string Unit_Id_05 { get; set; }
        [JsonProperty("unit_nm_05")]
        public string Unit_Nm_05 { get; set; }
        [JsonProperty("unit_id_06")]
        public string Unit_Id_06 { get; set; }
        [JsonProperty("unit_nm_06")]
        public string Unit_Nm_06 { get; set; }
        [JsonProperty("unit_id_07")]
        public string Unit_Id_07 { get; set; }
        [JsonProperty("unit_nm_07")]
        public string Unit_Nm_07 { get; set; }
        [JsonProperty("unit_id_08")]
        public string Unit_Id_08 { get; set; }
        [JsonProperty("unit_nm_08")]
        public string Unit_Nm_08 { get; set; }
        [JsonProperty("unit_id_09")]
        public string Unit_Id_09 { get; set; }
        [JsonProperty("unit_nm_09")]
        public string Unit_Nm_09 { get; set; }
        [JsonProperty("unit_id_10")]
        public string Unit_Id_10 { get; set; }
        [JsonProperty("unit_nm_10")]
        public string Unit_Nm_10 { get; set; }
        [JsonProperty("unit_base")]
        public string Unit_Base { get; set; }
        [JsonProperty("unit_start")]
        public string Unit_Start { get; set; }
        [JsonProperty("unit_end")]
        public string Unit_End { get; set; }
        [JsonProperty("unit_status")]
        public string Unit_Status { get; set; }        
    }
}
