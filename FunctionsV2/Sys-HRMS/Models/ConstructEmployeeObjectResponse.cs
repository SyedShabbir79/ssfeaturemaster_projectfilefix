using FunctionsV2.Helpers.Models;
using System.Collections.Generic;

namespace FunctionsV2.Sys_HRMS.Models
{
    public class ConstructEmployeeObjectResponse //: BaseResponse
    {
        public Employee Employee { get; set; }

        public List<EmployeeValidationError> ErrorList { get; set; } = new List<EmployeeValidationError>();
    }
}
