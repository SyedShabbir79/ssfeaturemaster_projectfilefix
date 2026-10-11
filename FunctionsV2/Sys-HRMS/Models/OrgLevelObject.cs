namespace FunctionsV2.Sys_HRMS.Models
{
    public class OrgLevelObject
    {
        public LevelDetails UoRStandard { get; set; }
        public LevelDetails UoRInternational { get; set; }
        public LevelDetails UoRAssociated { get; set; }
        public LevelDetails SUMS { get; set; }
    }

    public class LevelDetails
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

}
