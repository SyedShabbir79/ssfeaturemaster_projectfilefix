
//using Microsoft.Azure.Services.AppAuthentication;
using Azure.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;



/// <summary>
/// Jean Guo - 05/09/2023
/// OverseasStaff custom class used by HRMS_Calculate_OverseasStaff Azure function
/// </summary>
/// 


namespace FunctionsV2.Sys_HRMS
{
    public static class HRMS_OverseasStaff
    {

        public class OverseasStaff
        {
            public string error { get; set; }
            public bool hasError { get; set; } = false;
            public string staffCode { get; set; }
            public OverseasStaff (string school, bool onPayroll, string location)
            {                      
                // check for missing or invalid params                
                if (String.IsNullOrEmpty(location)) { error = "Missing location"; hasError = true; staffCode = null; }
            
                if (!hasError)
                {
                    switch(location)
                    { 
                     case "Finland":
                        staffCode = "F-P"; break;
                     case "Germany":
                        staffCode = "G-P"; break;
                     case "South Africa":
                        staffCode = "SA-P"; break;
                     case "Malaysia": 
                            {                                      
                                if (school == "Henley Business School UoRM" && onPayroll == false)                            
                                            staffCode = "MH-P";
                                    else if (school == "Henley Business School UoRM" && onPayroll == true)                            
                                            staffCode = "MH-UK"; 
                                             else if (school != "Henley Business School UoRM" && onPayroll == false)                           
                                                staffCode = "M-P"; 
                                                else if (school != "Henley Business School UoRM" && onPayroll == true)                            
                                                    staffCode = "M-UK";    
                              }; break;    
                                          
                    default:                                                                                                                                                                                                                
                            staffCode = "UK"; 
                            break;                                                                  
                    }
                }
            }
        }
    }
}

