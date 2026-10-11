using System.Collections.Generic;

namespace UoR.Sys.Staff.OrgData.Models
{
    public class OrgDataRequest
    {
        public OrgData OrgData { get; set; }
        public List<Approver> Approvers { get; set; }
    }
}