namespace FunctionsV2.Sys_Cop_Agresso
{    
    public class AgressoCopRequest
    {
        public string path { get; set; }
        public string op { get; set; }
        public Value value { get; set; }
        public bool supportNestedCollections { get; set; }
    }

    public class CoPResponseFx
    {
        public string cop_response_fx { get; set; }
    }

    public class Value
    {
        public CoPResponseFx copresponse { get; set; }
    }

}
