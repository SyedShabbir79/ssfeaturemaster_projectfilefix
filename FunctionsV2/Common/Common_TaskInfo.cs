namespace FunctionsV2.Common
{
    public class Common_TaskInfo
    {

        public Common_TaskInfo(string callbackUrl, string requestBody, string functionName)
        {
            this.CallbackUrl = callbackUrl;
            this.RequestBody = requestBody;
            this.FunctionName = functionName;
        }

        public string CallbackUrl { get; set; }
        public string RequestBody { get; set; }
        public string FunctionName { get; set; }
    }
}