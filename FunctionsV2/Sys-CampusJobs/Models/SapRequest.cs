using System.Collections.Generic;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    //Request payload for calling the SAP Campus Jobs API
    public class SapRequest
    {
        public Employee employee { get; set; }
    }

    public class Employee
    {
        public Personal personal { get; set; }
        public Address address { get; set; }
        public Employment employment { get; set; }
    }

    public class Personal
    {
        public string upn { get; set; }
        public string studentId { get; set; }
        public string staffId { get; set; }
        public string? title { get; set; }
        public string firstname { get; set; }
        public string lastName { get; set; }
        public string? businessFirstName { get; set; }
        public string? businessLastName { get; set; }
        public string? formalName { get; set; }
        public string? displayName { get; set; }
        public string? legalSex { get; set; }
        public string? nationality { get; set; }
        public string dateOfBirth { get; set; }
        public string email { get; set; }
    }

    public class Address
    {
        public string address1 { get; set; }
        public string locality { get; set; }
        public string city { get; set; }
        public string county { get; set; }
        public string postCode { get; set; }
        public string country { get; set; }
    }

    public class Employment
    {
        public PrimaryPosition primary { get; set; }
        public List<SecondaryPosition> secondary { get; set; }
    }
    public class MandatoryPositionDetails
    {
        public string serviceDate { get; set; }
        public string endDate { get; set; }
        public string positionCode { get; set; }
        public string positionPayScaleLevel { get; set; }
        public string startDate { get; set; }
        public string terminationReasonCode { get; set; }
    }

    public class PrimaryPosition : MandatoryPositionDetails
    {
        public string originalStartDate { get; set; }
        public string positionStartDate { get; set; }
    }

    public class SecondaryPosition : MandatoryPositionDetails
    {
        
    }    
}
