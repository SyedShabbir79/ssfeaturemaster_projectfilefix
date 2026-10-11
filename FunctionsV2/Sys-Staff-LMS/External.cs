using Microsoft.Extensions.DependencyModel.Resolution;
using Newtonsoft.Json.Linq;
using SIRSIDynix.Circulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FunctionsV2.Sys_LMS.Models
{
    public class External
    {
        public string username { get; set; }

        public string surname { get; set; }
        public string preferredName { get; set; }
        public string knownAsSurname { get; set; }
        public DateTime uniStartDate { get; set; }
        public DateTime? uniLeaveDate { get; set; }
        public string emailAddress { get; set; }
        public string upn { get; set; }


        public string userAccountStatus { get; set; }

        public DateTime? disabledDate { get; set; }
        public string positionTitle { get; set; }
        public string faculty { get; set; }
        public string school { get; set; }
        public string department { get; set; }
        public string unit { get; set; }
        public string managerRefNo { get; set; }
        public Applications applications { get; set; }

        public class Applications
        {
            public Lms lms { get; set; }
        }

        public class Lms
        {
            public bool? corpAccess { get; set; }
        }

        private Employee _ToEmployee = null;

        public Employee ToEmployee
        {
            get
            {
                if (_ToEmployee == null)
                {
                    _ToEmployee = new Employee
                    {
                        personal_reference = username,
                        username = username,
                        upn = upn,
                        email_address = emailAddress,
                    };
                }
                return _ToEmployee;
            }
        }
    }


    public class parseExternal
    {
        public External external;

        public parseExternal(JObject person)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            this.external = person.ToObject<External>();
        }

        public bool isValid
        {
            get
            {
                if (external == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(external.upn))
                {
                    return false;
                }


                return true;
            }
        }
    }
}
