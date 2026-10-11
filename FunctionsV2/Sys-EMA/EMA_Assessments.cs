using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using static FunctionsV2.Sys_EMA.EMA_Classes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using System.Linq.Expressions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

/// <summary>
/// Duncan Taylor - 10/02/2023
/// Helper method to parse assessment data from SITS and re-map to standard JSON structure
/// Vivek - 06/08/2024 -Rename EMA function names
/// </summary>

namespace FunctionsV2.Sys_EMA
{

    public static class EMA_Assessments
    {

        [FunctionName("EMA_ParseAssessment")]
        public static async Task<IActionResult> ParseAssessment([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function EMA_ParseAssessment invoked");

            // get data from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Byte[] newreq = Encoding.UTF8.GetBytes(requestBody);
            //string norm_reqBody = requestBody.Contains("\\n") ? requestBody.Replace("\\n", "") : requestBody;
            //string norm_reqBody = Regex.Replace(requestBody, @"\r\n|\r", "\n");
            string norm_reqBody = requestBody.Contains("\\n") ? requestBody.Replace("\\n", " ") : requestBody;
            if (String.IsNullOrWhiteSpace(requestBody))
                return new BadRequestObjectResult(error: new GradesResponse { error = "Missing request body", hasError = true });

            // get change type from request query string
            string changeType = req.Query["change_type"];
            if (String.IsNullOrWhiteSpace(changeType))
                return new BadRequestObjectResult(new MavRaw { error = "Missing change_type", hasError = true });

            // get change type from request query string
            string rootTable = req.Query["root_table"];
            if (String.IsNullOrWhiteSpace(rootTable))
                return new BadRequestObjectResult(new MavRaw { error = "Missing root_table", hasError = true });

            //log.LogInformation(JsonConvert.SerializeObject(requestBody, Formatting.Indented));
            log.LogInformation($"changeType: {changeType}");
            log.LogInformation($"rootTable: {rootTable}");

            MavRaw mavRaw = new MavRaw();
            string raw = "";

            // deserialize request body
            if (rootTable == "mav")
            {
                InMavRequest mavReq = JsonConvert.DeserializeObject<InMavRequest>(norm_reqBody);

                Mav outMav = GetMavByMav(mavReq.mavHeader.mav.mavCams);

                mavRaw.mav = outMav;

                raw = JsonConvert.SerializeObject(mavReq, Formatting.Indented);
            }

            else if (rootTable == "mab")
            {
                if (changeType == "UPDATE")
                {

                    InMabRequestUpdate mabReq = JsonConvert.DeserializeObject<InMabRequestUpdate>(norm_reqBody);

                    Mav outMav = GetMavByMab(mabReq.mabHeader.mab.mabCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(mabReq, Formatting.Indented);

                }

                else if (changeType == "CREATE")
                {
                    InMabRequestCreate mabReq = JsonConvert.DeserializeObject<InMabRequestCreate>(norm_reqBody);

                    Mav outMav = GetMavByMab(mabReq.mabHeader.mab.mabCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(mabReq, Formatting.Indented);
                }
                else if (changeType == "INSERT")
                {
                    InMabRequestCreate mabReq = JsonConvert.DeserializeObject<InMabRequestCreate>(norm_reqBody);

                    Mav outMav = GetMavByMab(mabReq.mabHeader.mab.mabCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(mabReq, Formatting.Indented);
                }


                else if (changeType == "DELETE")
                {
                    InMabRequestDelete mabReq = JsonConvert.DeserializeObject<InMabRequestDelete>(norm_reqBody);

                    Mav outMav = GetMavByMab(mabReq.mabHeader.mab.mabCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(mabReq, Formatting.Indented);
                }
            }

            else if (rootTable == "mad")
            {
                if (changeType == "UPDATE")
                {
                    InMadRequestUpdate madReq = JsonConvert.DeserializeObject<InMadRequestUpdate>(norm_reqBody);

                    Mav outMav = GetMavByMad(madReq.madHeader.mad.madCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(madReq, Formatting.Indented);
                }
                else if (changeType == "INSERT")
                {
                    InMadRequestCreate madReq = JsonConvert.DeserializeObject<InMadRequestCreate>(norm_reqBody);

                    Mav outMav = GetMavByMad(madReq.madHeader.mad.madCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(madReq, Formatting.Indented);
                }

                else if (changeType == "CREATE")
                {
                    InMadRequestCreate madReq = JsonConvert.DeserializeObject<InMadRequestCreate>(norm_reqBody);

                    Mav outMav = GetMavByMad(madReq.madHeader.mad.madCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(madReq, Formatting.Indented);
                }

                else if (changeType == "DELETE")
                {
                    InMadRequestDelete madReq = JsonConvert.DeserializeObject<InMadRequestDelete>(norm_reqBody);

                    Mav outMav = GetMavByMad(madReq.madHeader.mad.madCams);

                    mavRaw.mav = outMav;

                    raw = JsonConvert.SerializeObject(madReq, Formatting.Indented);
                }

                else
                {
                    return new BadRequestObjectResult(new MavRaw { error = "Unrecognised change_type", hasError = true });
                }

            }

            else
            {
                return new BadRequestObjectResult(new MavRaw { error = "Unrecognised root_table", hasError = true });
            }

            mavRaw.hasError = false;
            mavRaw.raw = raw;

            // return updated payload as a 200 response
            Console.WriteLine(raw);
            return new OkObjectResult(mavRaw);

        }

        private static Mav GetMavByMav(List<InRootMav> mavBody)
        {
            Mav outMav = null;

            if (mavBody != null)
            {
                List<Mav> outMavs = new List<Mav>();
                List<Mab> outMabs = new List<Mab>();
                List<Mad> outMads = new List<Mad>();
                foreach (InRootMav mav in mavBody)
                {
                    if (mav.mab != null)
                    {
                        foreach (InMabMad mabItem in mav.mab.mabCams)
                        {
                            if (mabItem.mad != null)
                            {
                                outMads = new List<Mad>();

                                foreach (InMad mad in mabItem.mad.madCams)
                                {
                                    outMads.Add(new Mad
                                    {
                                        mad_mod_code = mad.mad_mod_code,
                                        mad_mav_occur = mad.mad_mav_occur,
                                        mad_ayr_code = mad.mad_ayr_code,
                                        mad_psl_code = mad.mad_psl_code,
                                        mad_map_code = mad.mad_map_code,
                                        mad_mab_seq = mad.mad_mab_seq,
                                        mad_mad_ddate = mad.mad_mad_ddate,
                                        mad_mad_duet = mad.mad_mad_duet,
                                        mad_mab_type = "1"
                                    });
                                }

                            }

                            outMabs.Add(new Mab
                            {
                                mab_map_code = mabItem.mab_map_code,
                                mab_mab_seq = mabItem.mab_mab_seq,
                                mab_ast_code = mabItem.mab_ast_code,
                                mab_mab_perc = mabItem.mab_mab_perc,
                                mab_mab_tsha = mabItem.mab_mab_tsha,
                                mab_mab_name = mabItem.mab_mab_name,
                                mab_mks_code = mabItem.mab_mks_code,
                                mab_mab_agrp = mabItem.mab_mab_agrp,
                                mab_mab_rasi = mabItem.mab_mab_rasi,
                                mab_mab_udf1 = mabItem.mab_mab_udf1,
                                mab_mab_udfa = mabItem.mab_mab_udfa,
                                mab_mab_udfb = mabItem.mab_mab_udfb,
                                mab_mab_udfc = mabItem.mab_mab_udfc,
                                mab_mab_udfd = mabItem.mab_mab_udfd,
                                mab_mab_udfe = mabItem.mab_mab_udfe,
                                mab_mab_type = "1",
                                mab_mad = outMads.Count > 0 ? outMads : null
                            });
                        }

                    }

                    outMav = new Mav
                    {
                        mav_mod_code = mav.mav_mod_code,
                        mav_mav_occur = mav.mav_mav_occur,
                        mav_ayr_code = mav.mav_ayr_code,
                        mav_psl_code = mav.mav_psl_code,
                        mav_map_code = mav.mav_map_code,
                        mav_mabs = outMabs.Count > 0 ? outMabs : null
                    };
                    outMavs.Add(outMav);
                }
            }

            return outMav;
        }


        private static Mav GetMavByMab(List<InRootMab> mabBody)
        {
            // Initialize an empty list to hold Mabs
            List<Mab> outMabs = new List<Mab>();
            Mav outMav = null;

            // Iterate over each Mab item in the mabBody list
            foreach (InRootMab Mab in mabBody)
            {
                Mad outMad = null;

                // Populate the list of Mad objects if available in the current Mab item
                if (Mab.mad != null)
                {
                    List<Mad> madList = new List<Mad>();

                    foreach (var mad in Mab.mad.madCams)
                    {
                        outMad = new Mad
                        {
                            mad_mod_code = mad.mad_mod_code,
                            mad_mav_occur = mad.mad_mav_occur,
                            mad_ayr_code = mad.mad_ayr_code,
                            mad_psl_code = mad.mad_psl_code,
                            mad_map_code = mad.mad_map_code,
                            mad_mab_seq = mad.mad_mab_seq,
                            mad_mad_ddate = mad.mad_mad_ddate,
                            mad_mad_duet = mad.mad_mad_duet,
                            mad_mab_type = "1"
                        };

                        madList.Add(outMad);
                    }
                }

                // Create Mab instance and populate it with details from the current Mab item
                Mab mab = new Mab
                {
                    mab_map_code = Mab.mab_map_code,
                    mab_mab_seq = Mab.mab_mab_seq,
                    mab_ast_code = Mab.mab_ast_code,
                    mab_mab_perc = Mab.mab_mab_perc,
                    mab_mab_tsha = Mab.mab_mab_tsha,
                    mab_mab_name = Mab.mab_mab_name,
                    mab_mks_code = Mab.mab_mks_code,
                    mab_mab_agrp = Mab.mab_mab_agrp,
                    mab_mab_rasi = Mab.mab_mab_rasi,
                    mab_mab_udf1 = Mab.mab_mab_udf1,
                    mab_mab_udfa = Mab.mab_mab_udfa,
                    mab_mab_udfb = Mab.mab_mab_udfb,
                    mab_mab_udfc = Mab.mab_mab_udfc,
                    mab_mab_udfd = Mab.mab_mab_udfd,
                    mab_mab_udfe = Mab.mab_mab_udfe,
                    mab_mab_type = "1",
                    mab_mad = outMad != null ? new List<Mad> { outMad } : null
                };

                // Add the newly created Mab to the list of Mabs
                outMabs.Add(mab);
            }

            // Now process the list of Mabs to create the final Mav object, if available
            if (mabBody != null && mabBody.First().mav != null)
            {
                var firstMav = mabBody.First().mav.mavCams.FirstOrDefault();
                if (firstMav != null)
                {
                    outMav = new Mav
                    {
                        mav_mod_code = firstMav.mav_mod_code,
                        mav_mav_occur = firstMav.mav_mav_occur,
                        mav_ayr_code = firstMav.mav_ayr_code,
                        mav_psl_code = firstMav.mav_psl_code,
                        mav_map_code = firstMav.mav_map_code,
                        mav_mabs = outMabs
                    };
                }
            }

            // Return the Mav object containing all associated Mab entries
            return outMav;
        }

        private static Mav GetMavByMad(List<InRootMad> madBody)
        {
            // Initialize an empty list to hold Mad objects
            List<Mad> outMads = new List<Mad>();

            // Initialize an empty list to hold Mab objects
            List<Mab> outMabs = new List<Mab>();

            // Iterate over each `InRootMad` item in `madBody`
            foreach (var madItem in madBody)
            {
                // Create a Mad instance and populate it with properties from the current madItem
                Mad outMad = new Mad
                {
                    mad_mod_code = madItem.mad_mod_code,
                    mad_mav_occur = madItem.mad_mav_occur,
                    mad_ayr_code = madItem.mad_ayr_code,
                    mad_psl_code = madItem.mad_psl_code,
                    mad_map_code = madItem.mad_map_code,
                    mad_mab_seq = madItem.mad_mab_seq,
                    mad_mad_ddate = madItem.mad_mad_ddate,
                    mad_mad_duet = madItem.mad_mad_duet,
                    mad_mab_type = "1"
                };

                outMads.Add(outMad);

                // If there are Mab items within the current `madItem`, process them
                if (madItem.mab != null && madItem.mab.mabCams != null)
                {
                    foreach (var mab in madItem.mab.mabCams)
                    {
                        // Create a Mab instance and populate it with properties from `mab`
                        var mabInstance = new Mab
                        {
                            mab_map_code = mab.mab_map_code,
                            mab_mab_seq = mab.mab_mab_seq,
                            mab_ast_code = mab.mab_ast_code,
                            mab_mab_perc = mab.mab_mab_perc,
                            mab_mab_tsha = mab.mab_mab_tsha,
                            mab_mab_name = mab.mab_mab_name,
                            mab_mks_code = mab.mab_mks_code,
                            mab_mab_agrp = mab.mab_mab_agrp,
                            mab_mab_rasi = mab.mab_mab_rasi,
                            mab_mab_udf1 = mab.mab_mab_udf1,
                            mab_mab_udfa = mab.mab_mab_udfa,
                            mab_mab_udfb = mab.mab_mab_udfb,
                            mab_mab_udfc = mab.mab_mab_udfc,
                            mab_mab_udfd = mab.mab_mab_udfd,
                            mab_mab_udfe = mab.mab_mab_udfe,
                            mab_mab_type = "1",
                            mab_mad = new List<Mad> { outMad }
                        };

                        outMabs.Add(mabInstance);
                    }
                }
            }

            // Create and populate the `Mav` object outside the main loop, if `mavCams` exist
            Mav outMav = null;
            if (madBody != null && madBody.Any() && madBody.First().mav != null)
            {
                var firstMav = madBody.First().mav.mavCams.FirstOrDefault();
                if (firstMav != null)
                {
                    outMav = new Mav
                    {
                        mav_mod_code = firstMav.mav_mod_code,
                        mav_mav_occur = firstMav.mav_mav_occur,
                        mav_ayr_code = firstMav.mav_ayr_code,
                        mav_psl_code = firstMav.mav_psl_code,
                        mav_map_code = firstMav.mav_map_code,
                        mav_mabs = outMabs
                    };
                }
            }

            return outMav;
        }
        //Rename Function EMA_Grades_SendAssessment → EMA_SendAssessment
        [FunctionName("EMA_SendAssessment")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string))]
        public static async Task<IActionResult> SendAssessment([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {

            log.LogInformation("HTTP function EMA_SendAssessment invoked");

            // get data from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrWhiteSpace(requestBody))
                return new BadRequestObjectResult(error: new GradesResponse { error = "Missing request body", hasError = true });

            // deserialize request body
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            List<AssessmentIn> assessmentsIn = JsonConvert.DeserializeObject<List<AssessmentIn>>(requestBody, settings);
            if (assessmentsIn.IsNullOrDefault())
                return new BadRequestObjectResult(error: new GradesResponse { error = "Cannot get assessment payload from request body", hasError = true });
            if (assessmentsIn.Count < 1)
                return new BadRequestObjectResult(error: new GradesResponse { error = "Assessment payload doesn't contain any assessments", hasError = true });

            List<AssessmentOut> assessmentsOut = new List<AssessmentOut>();
            foreach (AssessmentIn assessment in assessmentsIn)
            {
                AssessmentOut assessmentOut = new AssessmentOut();
                assessmentOut.courseId = assessment.courseId;
                assessmentOut.itemType = assessment.itemType;
                assessmentOut.name = assessment.name;
                assessmentOut.optionalData = assessment.optionalData;
                assessmentOut.startDate = assessment.startDate;
                assessmentOut.dueDate = assessment.dueDate;
                assessmentOut.endDate = assessment.endDate;
                assessmentOut.points = assessment.points;
                assessmentOut.attempts = assessment.attempts;
                assessmentOut.gradeSchema = assessment.gradeSchema;

                if (assessment.group == 1)
                {
                    assessmentOut.group = "true";
                }
                else
                {
                    assessmentOut.group = "false";
                }

                if (assessment.available == 1)
                {
                    assessmentOut.available = "true";
                }
                else
                {
                    assessmentOut.available = "false";
                }

                if (assessment.extract == 1)
                {
                    assessmentOut.extract = "true";
                }
                else
                {
                    assessmentOut.extract = "false";
                }
                assessmentsOut.Add(assessmentOut);
            }

            //return new OkObjectResult(new AssessmentPayloadOut(assessmentsOut));
            return new OkObjectResult(assessmentsOut);
        }
    }
}