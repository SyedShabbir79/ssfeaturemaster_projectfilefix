using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Text.Json.Serialization;

namespace FunctionV2.Sys_REDCap
{
    public static class REDCap_GetUsernames
    {
        public class CompareUsernames_Request
        {
            public List<UAMDataClass> UAMData { get; set; }
            public List<REDCapUsernameData> REDCapData { get; set; }
        }

        public class CompareUsernames_Response
        {
            public List<ResponseData> disabledUsers { get; set; }
        }


        public class UAMDataClass
        {
            public string username { get; set; }
            public string identity_status { get; set; }
            public string surname { get; set; }
            public string preferred_name { get; set; }
            public DateTime? disabled_date { get; set; }
            public string job_title { get; set; }
            public DateTime? leave_date { get; set; }
            public DateTime? extension_date { get; set; }


            [JsonIgnore()]
            public string cleanUsername
            {
                get
                {
                    return username.Trim().ToLower();
                }
            }


        }

        public class ResponseData
        {
            public string username { get; set; }
            public string identity_status { get; set; }
            public string surname { get; set; }
            public string preferred_name { get; set; }
            public string disabled_date { get; set; }
            public string leave_date { get; set; }
        }

        public class REDCapUsernameData
        {
            public string user_username { get; set; }

            public string cleanUsername
            {
                get
                {
                    return user_username.Trim().ToLower();
                }
            }
        }

        [FunctionName("REDCap_CompareUsernames")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(CompareUsernames_Request))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CompareUsernames_Response))]
        public static async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            CompareUsernames_Request request = Newtonsoft.Json.JsonConvert.DeserializeObject<CompareUsernames_Request>(body);

            if (request.UAMData.Count > 0 && request.REDCapData.Count > 0)
            {
                List<ResponseData> unmatchedUsernames = new List<ResponseData>();

                foreach (var rcUser in request.REDCapData)
                {
                    var uamUser = request.UAMData.SingleOrDefault(uam => uam.cleanUsername.Equals(rcUser.cleanUsername));

                    if (uamUser == null || uamUser.identity_status != "Live")
                    {
                        unmatchedUsernames.Add(new ResponseData()
                        {
                            username = rcUser.user_username,
                            preferred_name = uamUser?.preferred_name ?? "Unknown",
                            surname = uamUser?.surname ?? "Unknown",
                            identity_status = uamUser?.identity_status ?? "Unknown, Probably Deleted",
                            disabled_date = uamUser?.disabled_date.GetValueOrDefault().ToString("dd/MM/yyyy") ?? "Unknown, Probably Deleted",
                            leave_date = "N/A"
                        });
                    }
                                        else if ((uamUser.extension_date.HasValue) || (uamUser.job_title == "Extended Leaver"))
                    {
                        string leaveDate = uamUser.leave_date?.ToString("dd/MM/yyyy")?? uamUser.extension_date?.ToString("dd/MM/yyyy") ?? uamUser.leave_date?.ToString("dd/MM/yyyy") ?? "Unknown";
                        unmatchedUsernames.Add(new ResponseData
                        {
                            username = rcUser.user_username,
                            preferred_name = uamUser.preferred_name,
                            surname = uamUser.surname,
                            identity_status = "Extended Leaver",
                            disabled_date = "N/A",
                            leave_date = leaveDate
                        });
                    }

                }

                return new OkObjectResult(new CompareUsernames_Response { disabledUsers = unmatchedUsernames });
            }
            return new BadRequestResult();
        }
    }
}
