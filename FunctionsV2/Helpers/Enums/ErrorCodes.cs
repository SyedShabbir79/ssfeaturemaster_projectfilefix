namespace FunctionsV2.Helpers
{
    public enum ErrorCodes
    {
        //Successes
        OK = 200,
        Created = 201,

        //Client Errors
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,        
        NotFound = 404,

        //Server Errors
        ServerError = 500,

        //Custom
        Unknown = 600
    }
}
