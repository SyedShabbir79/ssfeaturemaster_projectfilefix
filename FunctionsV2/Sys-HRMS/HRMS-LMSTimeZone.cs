//using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;



/// <summary>
/// Jean Guo - 05/09/2023
/// LMSTimeZone custom class used by HRMS_Calculate_LMSTimeZone Azure function
/// </summary>
/// 


namespace FunctionsV2.Sys_HRMS
{
    public static class HRMS_LMSTimeZone
    {

        public class LMSTimeZone
        {
            public string error { get; set; }
            public bool hasError { get; set; } = false;
            public string timeZone { get; set; }
            public string impsExclude { get; set; }
            public LMSTimeZone (string overseasStaff)
            {                      
                // check for missing or invalid params
                if (String.IsNullOrEmpty (overseasStaff)) { error = "Missing overseas_staff"; hasError = true; timeZone = null; }
            
                if (!hasError)
                {
                    switch(overseasStaff)
                    { 
                     case "F-P":
                        timeZone = "tzone000000000000022"; 
                        impsExclude = "false"; 
                        break;
                     case "G-P":
                        timeZone = "tzone000000000000023"; 
                        impsExclude = "false"; 
                        break;
                     case "SA-P":
                        timeZone = "tzone000000000000030"; 
                        impsExclude = "true";  
                        break;
                     case "MH-UK" or "MH-P" or "M-UK" or "M-P":
                        timeZone = "tzone000000000000046"; 
                        impsExclude = "true";  
                        break;
                     default:                           
                        timeZone = "tzone000000000000022";
                        impsExclude = "false";
                        break;
                    }
                            
                    }
                }
            }
        }
}

