using Microsoft.Extensions.DependencyModel.Resolution;
using Newtonsoft.Json.Linq;
using SIRSIDynix.Circulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FunctionsV2.Sys_LMS.Models
{
    public class Employee
    {
        public string personal_reference { get; set; }
        public string username { get; set; }


        public string upn { get; set; }

        public string staff_type { get; set; }
        public string prefered_name { get; set; }
        public string known_as_surname { get; set; }

        public DateTime? employee_start_date { get; set; }
        public DateTime? employee_end_date { get; set; }



        //public string address_line1 { get; set; }
        //public string address_line2 { get; set; }
        //public string city { get; set; }
        //public string county { get; set; }
        //public string post_code { get; set; }


        public string email_address { get; set; }
        public string overseas_staff { get; set; }

        public class EmployeePosition
        {
            public class UnitClass
            {
                public string leaf { get; set; }
                public string unit_nm_01 { get; set; }
                public string unit_nm_02 { get; set; }
                public string unit_nm_03 { get; set; }
                public string unit_nm_04 { get; set; }
                public string unit_nm_05 { get; set; }
                public string unit_nm_06 { get; set; }
                public string unit_nm_07 { get; set; }
                public string unit_nm_08 { get; set; }
                public string unit_nm_09 { get; set; }
                public string unit_nm_10 { get; set; }
                public string unit_base { get; set; }
                public DateTime unit_start { get; set; }
                public string unit_id_01 { get; set; }
                public string unit_id_02 { get; set; }
                public string unit_id_03 { get; set; }
                public string unit_id_04 { get; set; }
                public string unit_id_05 { get; set; }
                public string unit_id_06 { get; set; }
                public string unit_id_07 { get; set; }
                public string unit_id_08 { get; set; }
                public string unit_id_09 { get; set; }
                public string unit_id_10 { get; set; }
                public string unit_status { get; set; }

                public string baseParentLeaf
                {
                    get
                    {
                        return unit_id_04 ?? unit_id_03 ?? unit_id_02;
                    }
                }
            }

            public UnitClass position_unit { get; set; }

            public string occupancy_id { get; set; }

            public string faculty { get; set; }
            public string school { get; set; }
            public int main_position { get; set; }


            public string position_title { get; set; }

            public string pay_category { get; set; }

            public string manager_ref_no { get; set; }


        }

        public List<EmployeePosition> positions { get; set; }



        public List<string> nonPrimaryManagers
        {
            get
            {
                if (currentPositions.Count(p => p.main_position < 100 && p.main_position > 1) > 0)
                {
                    return currentPositions.Where(p => p.main_position < 100 && p.occupancy_id != mainPosition.occupancy_id && p.manager_ref_no != mainPosition.manager_ref_no && p.manager_ref_no != personal_reference && !string.IsNullOrWhiteSpace(p.manager_ref_no)).Select(x => x.manager_ref_no).ToList();
                }
                return new List<string>();
            }
        }





        public List<EmployeePosition> currentPositions
        {
            get
            {
                return positions.Where(p => isCurrentPosition(p)).ToList();
            }
        }

        private bool isCurrentPosition(EmployeePosition p)
        {
            if (!string.IsNullOrWhiteSpace(p.faculty))
            {
                if (!string.IsNullOrWhiteSpace(p.position_title))
                {
                    if (staff_type == "E" || staff_type == "L" || staff_type == "X" || (staff_type == "W" && p.pay_category == "Secondee"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public EmployeePosition mainPosition
        {
            get
            {
                if (currentPositions.Count > 0)
                {
                    return currentPositions.OrderBy(p => p.main_position).FirstOrDefault();
                    //int minPosition = currentPositions.Min(p => p.main_position);
                    //return currentPositions.SingleOrDefault(p => p.main_position == minPosition);
                }

                return null;


            }
        }



        public applicationClass applications { get; set; }

        public class applicationClass
        {
            public class lmsClass
            {
                public string timezone { get; set; }
                public bool impsexclude { get; set; }

            }
            public lmsClass lms { get; set; }
        }
    }

    public class parseEmployee
    {
        public Employee employee;

        public parseEmployee(JObject person)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            this.employee = person.ToObject<Employee>();
        }







        public bool isValid
        {
            get
            {
                if (employee == null
                    || string.IsNullOrWhiteSpace(employee.personal_reference)
                    || string.IsNullOrWhiteSpace(employee.username)
                    || string.IsNullOrWhiteSpace(employee.staff_type)
                    || string.IsNullOrWhiteSpace(employee.upn)
                    || employee.mainPosition == null
                    || employee.applications.lms == null)

                    return false;
                return true;
            }
        }
    }
}
