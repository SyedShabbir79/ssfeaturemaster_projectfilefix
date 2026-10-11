using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionsV2.Sys_SymLib.Models
{
    public class StaffClass
    {
        public string personal_reference { get; set; }
        public string username { get; set; }
        public string staff_type { get; set; }
        public string prefered_name { get; set; }
        public string known_as_surname { get; set; }
        public DateTime? uni_leave_date { get; set; }
        public string address_line1 { get; set; }
        public string address_line2 { get; set; }
        public string city { get; set; }
        public string county { get; set; }
        public string post_code { get; set; }
        public string email_address { get; set; }
        public string overseas_staff { get; set; }

        public class StaffPosition
        {
            public string faculty { get; set; }
            public string school { get; set; }
            public int main_position { get; set; }

            public DateTime? pos_occ_end { get; set; }

            public string FacultyorSchoolName
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(school))
                    {
                        return faculty;
                    }
                    return school;
                }
            }
        }

        public List<StaffPosition> positions { get; set; }

        public StaffPosition MainPosition
        {
            get
            {
                int minPosition = positions.Min(p => p.main_position);
                return positions.Where(p => p.main_position == minPosition).OrderBy(p => p.pos_occ_end.GetValueOrDefault()).FirstOrDefault();
            }
        }



        public applicationClass applications { get; set; }

        public class applicationClass
        {
            public class dttClass
            {
                public string risis_code { get; set; }

            }
            public dttClass dtt { get; set; }
        }
    }

    //public class StaffClass
    //{
    //    public string personal_reference { get; set; }
    //    public string username { get; set; }
    //    public string staff_type { get; set; }
    //    public string prefered_name { get; set; }
    //    public string known_as_surname { get; set; }
    //    public DateTime? uni_leave_date { get; set; }
    //    public string address_line1 { get; set; }
    //    public string address_line2 { get; set; }
    //    public string city { get; set; }
    //    public string county { get; set; }
    //    public string post_code { get; set; }
    //    public string email_address { get; set; }
    //    public string overseas_staff { get; set; }
    //    public List<StaffPosition> positions { get; set; }

    //    public StaffPosition MainPosition
    //    {
    //        get
    //        {
    //            int minPosition = positions.Min(p => p.main_position);
    //            return positions.Where(p => p.main_position == minPosition).OrderBy(p => p.pos_occ_end.GetValueOrDefault()).FirstOrDefault();
    //        }
    //    }

    //    public applicationClass applications { get; set; }

    //    public class applicationClass
    //    {
    //        public class dttClass
    //        {
    //            public string risis_code { get; set; }

    //        }
    //        public dttClass dtt { get; set; }
    //    }
    //}

    //public partial class StaffPosition
    //{
    //    public string faculty { get; set; }
    //    public string school { get; set; }
    //    public int main_position { get; set; }

    //    public DateTime? pos_occ_end { get; set; }

    //    public string FacultyorSchoolName
    //    {
    //        get
    //        {
    //            if (string.IsNullOrWhiteSpace(school))
    //            {
    //                return faculty;
    //            }
    //            return school;
    //        }
    //    }
    //}
}
