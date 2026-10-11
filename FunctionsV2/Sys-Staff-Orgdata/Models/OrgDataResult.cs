namespace UoR.Sys.Staff.OrgData.Models
{
    public class OrgDataResult
    {
        public string Faculty { get; set; }
        public string FacultyId { get; set; }
        public string School { get; set; }
        public string SchoolId { get; set; }
        public string Dept { get; set; }
        public string DeptId { get; set; }
        public string Unit { get; set; }
        public string UnitId { get; set; }
        public string SubUnit { get; set; }
        public string SubUnitId { get; set; }
        public Approvers Approver { get; set; }
        public bool Change { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string ChangedUnitId { get; set; }
        public string ChangedUnitName { get; set; }
        public string ParentId { get; set; }
    }
}