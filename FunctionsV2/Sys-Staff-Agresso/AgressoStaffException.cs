using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_Staff_Agresso
{
    public class AgressoStaffException : Exception
    {
        public string functionName { get; }


        public AgressoStaffException(string message) : base(message) { }


        public AgressoStaffException(string function, object message) : this(JsonConvert.SerializeObject(message))
        {
            this.functionName = function;
        }




        //public AgressoStaffException(string function, object Error) : base(new { function, error = JsonConvert.SerializeObject(Error) }) { }

        //public AgressoStaffException(string Error) : base(Error) { }
    }
}
