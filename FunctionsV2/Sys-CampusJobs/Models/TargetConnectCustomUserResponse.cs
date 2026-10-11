using System;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    //Payload returned from Target Connect GetCustomUser API
    //GET API
    public class TargetConnectCustomUserResponse
    {
        public string id { get; set; }
        public string username { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public Group[] groups { get; set; }
        public string importedId { get; set; }
        public DateTime createdOn { get; set; }
        public string createdBy { get; set; }
        public bool active { get; set; }
        public string userType { get; set; }
        public Customfields customFields { get; set; }
        public string status { get; set; }
    }

    public class Customfields
    {
        public UNIVERSITY_NUMBER UNIVERSITY_NUMBER { get; set; }
        public REF365 REF365 { get; set; }
        public ADDRESS1 ADDRESS1 { get; set; }
        public ADDRESS4 ADDRESS4 { get; set; }
        public COUNTRY_OF_DOMICILE COUNTRY_OF_DOMICILE { get; set; }
        public DATE_OF_BIRTH DATE_OF_BIRTH { get; set; }
        public REF3590 REF3590 { get; set; }
        public GENDER GENDER { get; set; }
        public VALUE41 VALUE41 { get; set; }
    }

    public class UNIVERSITY_NUMBER
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value 
        {
            public string text { get; set; }
        }
    }

    public class VALUE41
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
        }
    }

    public class REF365
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
            public string code { get; set; }
        }
    }

    public class GENDER
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
            public string code { get; set; }
        }
    }

    public class ADDRESS1
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
        }
    }

    public class ADDRESS4
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
        }
    }

    public class COUNTRY_OF_DOMICILE
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
        }
    }

    public class DATE_OF_BIRTH
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
            public string date { get; set; }
        }
    }

    public class REF3590
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
            public string code { get; set; }
        }
    }   
    public class Trentstatus
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
            public string code { get; set; }
        }
    }

    public class User
    {
        public string id { get; set; }
        public string fullName { get; set; }
        public string preferredFullName { get; set; }
        public string operatorPreferredFullName { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public string name { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public bool active { get; set; }
        public string userAccessType { get; set; }
    }

    public class VALUE3594
    {
        public string name { get; set; }
        public string fieldType { get; set; }
        public int order { get; set; }
        public Value value { get; set; }

        public partial class Value
        {
            public string text { get; set; }
        }
    }

    public class Group
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}
