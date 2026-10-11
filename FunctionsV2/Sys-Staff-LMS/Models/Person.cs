using System;

namespace UoR.Sys.LMS.UoRLearn.Models
{
    public class Person
    {
        public Securitydomain securityDomain { get; set; }
        public Customvalues customValues { get; set; }
        public string requiredJobRoles { get; set; }
        public string optionalJobRoles { get; set; }
        public string positions { get; set; }
        public string securityRoles { get; set; }
        public string audiencetypes { get; set; }
        public DateTime? updated_on { get; set; }
        public DateTime? created_on { get; set; }
        public DateTime? job_start_date { get; set; }
        public string title { get; set; }
        public string email { get; set; }
        public int gender { get; set; }
        public string city { get; set; }
        public string fax { get; set; }
        public string zip { get; set; }
        public string addr1 { get; set; }
        public string addr2 { get; set; }
        public string addr3 { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public Location_Id location_id { get; set; }
        public string password { get; set; }
        public string suffix { get; set; }
        public string status { get; set; }
        public string username { get; set; }
        public string id { get; set; }
        public object job_title_type { get; set; }
        public Home_Company_Id home_company_id { get; set; }
        public bool special_user { get; set; }
        public DateTime? terminated_on { get; set; }
        public Home_Domain home_domain { get; set; }
        public bool is_manager { get; set; }
        public string job_title { get; set; }
        public bool password_changed { get; set; }
        public Company_Id company_id { get; set; }
        public JobType jobtype_id { get; set; }
        public string ss_no { get; set; }
        public string person_type { get; set; }
        public string fname { get; set; }
        public string lname { get; set; }
        public string workphone { get; set; }
        public string homephone { get; set; }
        public DateTime? date_of_birth { get; set; }
        public string person_no { get; set; }
        public string mname { get; set; }
        public DateTime? started_on { get; set; }
        public bool is_508_user { get; set; }
        public Desired_Job_Type desired_job_type_id { get; set; }
        public string secret_question { get; set; }
        public string secret_answer { get; set; }
        public string correspondence_preference1 { get; set; }
        public string correspondence_preference2 { get; set; }
        public string correspondence_preference3 { get; set; }
        public Timezone_Id timezone_id { get; set; }
        public Locale_Id locale_id { get; set; }
        public Manager_Id manager_id { get; set; }

        public Association associations { get; set; }
    }

    public class Association
    {
        public Resource resource { get; set; }

        public class Resource
        {
            public bool markResource { get; set; }
            public RateClass[] rate { get; set; }

            public class RateClass
            {
                public string amount { get; set; }
                public Currency_Id currency { get; set; }
            }
        }
    }


    public class Desired_Job_Type
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }


    public class JobType
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }



    public class Location_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Home_Company_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Home_Domain
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Company_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Timezone_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Locale_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Manager_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Securitydomain
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }



    public class Currency_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Parent_Id
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Currency
    {
        public string id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
    }

    public class Customvalues
    {
        public string custom0 { get; set; }
        public string custom1 { get; set; }
        public string custom2 { get; set; }
        public string custom3 { get; set; }
        public object custom4 { get; set; }
        public object custom5 { get; set; }
        public object custom6 { get; set; }
        public object custom7 { get; set; }
        public object custom8 { get; set; }
        public object custom9 { get; set; }
        public object custom10 { get; set; }
        public object custom11 { get; set; }
        public object custom12 { get; set; }
        public object custom13 { get; set; }
        public object custom14 { get; set; }
        public object custom15 { get; set; }
    }
}
