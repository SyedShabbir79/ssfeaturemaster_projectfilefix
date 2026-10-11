using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FunctionV2.Sys_Staff_Agresso
{
    public partial class Employee
    {
        public Position mainPosition
        {
            get
            {
                int minPosition = positions.Min(p => p.main_position);
                return positions.Single(p => p.main_position == minPosition);
            }
        }


        public string Company
        {
            get
            {
                switch (overseas_staff)
                {
                    case "M-P":
                    case "MH-P":
                        return "RM";

                    default:
                        return "RU";
                }
            }
        }


    }

    public partial class Employee
    {
        public string personal_reference { get; set; }
        public string username { get; set; }
        public string upn { get; set; }
        public string email_address { get; set; }
        public string staff_type { get; set; }
        public string prefered_name { get; set; }
        public string known_as_surname { get; set; }

        public DateTime uni_start_date { get; set; }
        public DateTime? uni_leave_date { get; set; }
        public string pay_point { get; set; }

        public string overseas_staff { get; set; }


        public string bank_account { get; set; }
        public string sort_code { get; set; }
        public string roll_number { get; set; }

        public string post_code { get; set; }
        public string country { get; set; }

        public string work_internal_phone { get; set; }

        public List<Position> positions { get; set; }

        public class Position
        {
            public string occupancy_id { get; set; }
            public string occupancy_reference { get; set; }
            public string position_reference { get; set; }
            public string pay_category { get; set; }
            public string faculty { get; set; }
            public string school { get; set; }
            public string department { get; set; }
            public string base_unit { get; set; }
            public string location { get; set; }
            public string pos_occ_start { get; set; }
            public object pos_occ_end { get; set; }
            public int main_position { get; set; }
            public string manager_ref_no { get; set; }
            public string position_title { get; set; }
            public string position_status { get; set; }
            public string position_type { get; set; }
            public object pos_occ_exp_end { get; set; }
            public string position_unit_id { get; set; }
        }






    }
}