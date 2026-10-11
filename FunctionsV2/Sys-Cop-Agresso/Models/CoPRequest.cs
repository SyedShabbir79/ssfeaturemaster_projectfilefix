namespace FunctionsV2.Sys_Cop_Agresso.Models
{
    public class CoPRequest
    {        
        public LbgRequest lbgRequest { get; set; }     
        
        public string SupplierId { get; set; }
    }

    public class LbgRequest
    {
        public Account account { get; set; }
    }

    public class Account
    {
        public string sortCode { get; set; }
        public string accountNumber { get; set; }
        public string name { get; set; }
        public string accountType { get; set; }
        public string identifier { get; set; }
    }
}
