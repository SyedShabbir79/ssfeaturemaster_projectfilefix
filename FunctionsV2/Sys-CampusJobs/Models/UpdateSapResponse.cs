using FunctionsV2.Helpers.Models;

namespace FunctionsV2.Sys_CampusJobs.Models
{
    public class UpdateSapResponse : BaseResponse
    {
        public TargetConnectCustomerUserPatch TargetConnectRequestPayload { get; set; }
    }
}
