namespace FunctionsV2.Sys_SymLib.Models
{
    public class UpsertRequest
    {
        public string ClientId { get; set; }

        public string Login { get; set; }

        public string OverrideCode { get; set; }

        public object RequestObject { get; set; }

        public string DataSource { get; set; }
    }
}
