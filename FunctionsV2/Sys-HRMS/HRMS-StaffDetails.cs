using System;

/// <summary>
/// Duncan Taylor - 22/08/2023
/// Staff Details Custom Class Used by HRMS_Calculate_StaffDetails Azure Function
/// </summary>
/// 


namespace FunctionsV2.Sys_HRMS
{
    public static class HRMS_StaffDetails
    {

        public class StaffDetails
        {
            public string location { get; set; }
            public string payCategory { get; set; }
            public string overseasStaff { get; set; }
            public string error { get; set; }
            public bool hasError { get; set; } = false;
            public StaffDetails(string loc, string payCat, string overseasS, bool hasErr)
            {
                location = loc;
                payCategory = payCat;
                overseasStaff = overseasS;
                hasError = hasErr;
            }
            public StaffDetails(string loc, string payCat, string overseasS, bool hasErr, string err)
            {
                location = loc;
                payCategory = payCat;
                overseasStaff = overseasS;
                hasError = hasErr;
                error = err;
            }
        }

        public class StaffDetailsRequest
        { 
            public string employeeGroup { get; set; }
            public string employeeSubgroup { get; set; }
            public string paygrade { get; set; }
            public string locationCode { get; set; }
            public string faculty { get; set; }
            public string school { get; set; }
            public string base_unit { get; set; }
            public bool on_payroll { get; set; }
            
        }
    }
}
