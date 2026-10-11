using Microsoft.Azure.WebJobs.Host;
using System;
using System.Reactive;

namespace FunctionsV2.Sys_Staff_Trent_Api_Log.Models
{

    public class TrentApiResponse
    { 
        public Notification Notification { get; set; }
    }
    
    public class Notification
    {
        public string TransactionReference { get; set; }
        public string SuccessfulRecords { get; set; }
        public string ErrorRecords { get; set; }
        public string IsSuccessful { get; set; }
        public Errors Errors { get; set; }
    }

    public class Errors
    {
        public Error Error { get; set; }
    }

    public class Error
    {
        public string ErrorDescription { get; set; }
    }

}
