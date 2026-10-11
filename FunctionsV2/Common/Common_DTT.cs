using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Data;

namespace FunctionsV2.Common
{
    public static class Common_DTT
    {
        public class dttEntry
        {
            public string trent_faculty { get; set; }
            public string trent_school { get; set; }
            public string trent_department { get; set; }

            public string faculty_directorate_code { get; set; }
            public string its_code { get; set; }
            public string risis_code { get; set; }

            public string primary_email_domain { get; set; }
            public string secondary_email_domain { get; set; }

            public bool isActive { get; set; }

            public int dtt_rank { get; set; }
        }

        public class dttEnrichRequst
        {
            public dttEntry[] dtt { get; set; }
            public object person { get; set; }
        }

        public class employee
        {
            public class position
            {
                public string faculty { get; set; }
                public string school { get; set; }
                public string department { get; set; }
                public int main_position { get; set; }
            }

            public position[] positions { get; set; }

            public position main_position
            {
                get
                {
                    return positions.OrderBy(p => p.main_position).FirstOrDefault();
                }
            }

        }



        public class dttEnrichResponse
        {
            public bool foundDTT { get; set; }
            public object person { get; set; }
        }


        private static string UpperOrNull(string string1)
        {
            if (String.IsNullOrWhiteSpace(string1))
            {
                return "*NULL*";
            }
            return string1.ToUpper();
        }



        [FunctionName("Common_DTT_Enrich")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(dttEnrichRequst))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(dttEnrichResponse))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dttEnrichRequst request = Newtonsoft.Json.JsonConvert.DeserializeObject<dttEnrichRequst>(requestBody);

            //employee e = request.person.ToObject<employee>();

            JToken person = JToken.FromObject(request.person);
            employee e = person.ToObject<employee>();


            if (e.main_position != null)
            {
                dttEntry lookup = request.dtt.Where(
                    d => UpperOrNull(d.trent_faculty) == UpperOrNull(e.main_position.faculty)
                    && UpperOrNull(d.trent_school) == UpperOrNull(e.main_position.school)
                    && UpperOrNull(d.trent_department) == UpperOrNull(e.main_position.department)
                ).OrderBy(d => d.isActive).ThenBy(d => d.dtt_rank).FirstOrDefault();

                if (lookup != null)
                {
                    var dtt = new JProperty("dtt", JObject.FromObject(new
                    {
                        lookup.faculty_directorate_code,
                        lookup.its_code,
                        lookup.risis_code,
                        lookup.primary_email_domain,
                        lookup.secondary_email_domain
                    }));


                    JObject applications = person.SelectToken("applications") as JObject;
                    if (applications == null)
                    {
                        person.Last.AddAfterSelf(new JProperty("applications", new JObject(dtt)));
                    }
                    else
                    {
                        person.SelectToken("applications").Last.AddAfterSelf(dtt);
                    }

                    return new OkObjectResult(new dttEnrichResponse() { person = person, foundDTT = true });
                }
                else
                {
                    var dtt = new JProperty("dtt", null);

                    JObject applications = person.SelectToken("applications") as JObject;
                    if (applications == null)
                    {
                        person.Last.AddAfterSelf(new JProperty("applications", new JObject(dtt)));
                    }
                    else
                    {
                        applications.Last.AddAfterSelf(dtt);
                    }
                }
            }
            return new OkObjectResult(new dttEnrichResponse() { person = person, foundDTT = false });
        }
    }
}
