using System;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    //Used when calling SP [dbo].[UoR_AIS_GetCJPlacementRecord]
    public class GetPlacementRecord
    {
        public string StaffID { get; set; }

        public string StudentID { get; set; }

        public string StaffPositionID { get; set; }

        public DateTime PositionStartDate { get; set; }

        public DateTime? PositionEndDate { get; set; }

        public string? PayScaleLevel { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? IntegrationStatus { get; set; }

        public int? Status { get; set; }

        public string ApplicationId { get; set; }

        public string? Username { get; set; }
    }

}

