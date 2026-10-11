using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Helpers
{
    public static class BodyReader
    {
        public static async Task<T> Read<T>(Stream body)
        {
            StreamReader r = new StreamReader(body);
            string stringBody = await r.ReadToEndAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(stringBody);
        }
    }
}
