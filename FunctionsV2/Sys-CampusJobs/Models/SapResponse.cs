using Newtonsoft.Json;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    //Response payload from SAP Campus Jobs API - TBC

    public class SapResponse
    {
        public string upn { get; set; }
        public string studentId { get; set; }
        public string staffId { get; set; }
        public SapCJPosition[] positions { get; set; }
    }

    public class SapCJPosition
    {
        public string position { get; set; }
        public Step[] steps { get; set; }
    }

    public class Step
    {
        public int step { get; set; }
        public string status { get; set; }
        public string location { get; set; }
        public string message { get; set; }
    }

}

