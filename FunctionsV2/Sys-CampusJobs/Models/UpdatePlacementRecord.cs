using System;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    //Used when calling SP [dbo].[UoR_AIS_UpdateCJPlacementRecord]
    public class UpdatePlacementRecord
    {
        public string StaffID { get; set; }

        public string StudentID { get; set; }

        public string SapPositionID { get; set; }

        public DateTime PositionStartDate { get; set; }

        public DateTime? PositionEndDate { get; set; }

        public string? PayScaleLevel { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string Username { get; set; }

    }
}
