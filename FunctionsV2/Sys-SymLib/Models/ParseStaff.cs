using System;

namespace FunctionsV2.Sys_SymLib.Models
{
    public class ParseStaff
    {
        public StaffClass person;

        public ParseStaff(StaffClass person)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            this.person = person;
        }

        public bool isValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(person.personal_reference)
                    || string.IsNullOrWhiteSpace(person.username)
                    || string.IsNullOrWhiteSpace(person.staff_type)
                    || person.MainPosition == null
                    || person.applications.dtt == null)

                    return false;
                return true;
            }
        }
    }
}
