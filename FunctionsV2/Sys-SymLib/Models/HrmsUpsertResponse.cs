using FunctionsV2.Helpers.Models;
using SIRSIDynix.Circulation;

namespace FunctionsV2.Sys_SymLib.Models
{
    public class HrmsUpsertResponse : BaseFunctionResponseError
    {

        public User User { get; set; }

    }
}
