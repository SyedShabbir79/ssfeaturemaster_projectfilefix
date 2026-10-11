namespace FunctionsV2.Helpers.Models
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class BaseFunctionResponse
    {        
        public BaseFunctionResponseError Error { get; set; }

        public object Response { get; set; }  
    }

    public class BaseFunctionResponseError
    {
        public string Code { get; set; }

        public string Message { get; set; }
    }
}

