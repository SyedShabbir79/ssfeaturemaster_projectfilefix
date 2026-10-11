using Newtonsoft.Json;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class PositionError
    {
        [JsonProperty("pos_ref")]
        public string Pos_Ref { get; set; }
        [JsonProperty("error_text")]
        public string ErrorText { get; set; }
        public PositionError(string posRef, string errorText)
        {
            this.Pos_Ref = posRef;
            this.ErrorText = errorText;
        }
    }
}
