namespace FunctionsV2.Sys_SymLib.Models
{
    public class GetRequest
    {
        public string ClientId { get; set; }

        public string Login { get; set; }

        public UserDetails UserDetails { get; set; }
       
    }

    public partial class UserDetails
    {
        public string PersonalReference { get; set; }

        public string Username { get; set; }
    }
}
