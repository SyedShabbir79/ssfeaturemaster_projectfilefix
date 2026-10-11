namespace FunctionsV2.Sys_CampusJobs.Models
{
    public class TargetConnectCustomerUserPatch
    {
        //Request payload to Target Connect GetCustomUser API
        //PATCH API
        public string Username { get; set; }

        public CustomFields[] CustomFields { get; set; }
    }

    public class CustomFields()
    {
        public VALUE41 VALUE41 {  get; set; }

        public Trentstatus SapStatus { get; set; }
    }

}
