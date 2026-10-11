using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace FunctionsV2.Helpers.Models
{
    public class JwtRequest
    {
        public string KeyId { get; set; }

        public Dictionary<string, object> Payload { get; set; }

        public X509Certificate2 Certificate { get; set; }
    }
}
