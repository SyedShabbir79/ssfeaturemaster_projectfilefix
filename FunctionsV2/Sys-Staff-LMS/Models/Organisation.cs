using System;
using System.Collections.Generic;
using System.Text;

namespace UoR.Sys.LMS.UoRLearn.Models
{
    public class Organisation : LMSBase
    {
        public Securitydomain securityDomain { get; set; }
        public Customvalues customValues { get; set; }
        public string billAddr1 { get; set; }
        public string billAddr2 { get; set; }
        public string billCity { get; set; }
        public string billState { get; set; }
        public string billZip { get; set; }
        public string billCountry { get; set; }
        public Currency[] currencies { get; set; }
        public object vertical { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string country { get; set; }
        public object description { get; set; }
        public string state { get; set; }
        
        public object objectives_administrator { get; set; }
        public string email { get; set; }
        public object contact_id { get; set; }
        public string web_server { get; set; }
        public string fax { get; set; }
        public string cost_center_no { get; set; }
        public string phone2 { get; set; }
        public string phone1 { get; set; }
        public string number { get; set; }
        public object aa_id { get; set; }
        public string addr1 { get; set; }
        public string addr2 { get; set; }
        public string addr3 { get; set; }
        public Currency_Id currency_id { get; set; }
        
        public Parent_Id parent_id { get; set; }
    }
}
