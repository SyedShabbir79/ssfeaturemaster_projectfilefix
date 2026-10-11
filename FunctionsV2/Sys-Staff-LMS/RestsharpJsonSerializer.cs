using Newtonsoft.Json;
using System.IO;
using RestSharp.Serializers;
using RestSharp;

namespace FunctionsV2.Sys_Staff_LMS
{
    public class RestsharpJsonSerializer : ISerializer, IDeserializer
    {
        private Newtonsoft.Json.JsonSerializer serializer;

        public RestsharpJsonSerializer(Newtonsoft.Json.JsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public string ContentType
        {
            get { return "application/json"; }
            set { }
        }

        public string DateFormat { get; set; }

        public string Namespace { get; set; }

        public string RootElement { get; set; }

        public string Serialize(object obj)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var jsonTextWriter = new JsonTextWriter(stringWriter))
                {
                    serializer.Serialize(jsonTextWriter, obj);

                    return stringWriter.ToString();
                }
            }
        }

        public T Deserialize<T>(RestSharp.RestResponse response)
        {
            var content = response.Content;

            using (var stringReader = new StringReader(content))
            {
                using (var jsonTextReader = new JsonTextReader(stringReader))
                {
                    return serializer.Deserialize<T>(jsonTextReader);
                }
            }
        }

        public static RestsharpJsonSerializer Default
        {
            get
            {
                return new RestsharpJsonSerializer(new Newtonsoft.Json.JsonSerializer()
                {
                    NullValueHandling = NullValueHandling.Include,
                });
            }
        }

        ContentType ISerializer.ContentType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}