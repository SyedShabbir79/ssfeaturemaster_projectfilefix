using System;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    public class PlacementRecord
    {
        public string? StaffID { get; set; }

        public string StudentID { get; set; }

        public string TargetConnectID { get; set; }

        public string? SapPositionID { get; set; }

        public string? ApplicationID { get; set; }

        public DateTime PositionStartDate { get; set; }

        public DateTime PositionEndDate { get; set; }

        public string PayScaleLevel { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string Username { get; set; }
    }

}
