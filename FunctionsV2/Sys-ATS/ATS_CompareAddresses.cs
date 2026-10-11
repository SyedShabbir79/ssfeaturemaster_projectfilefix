using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using OfficeOpenXml;
using System.Text;

namespace FunctionsV2.Sys_ATS
{
    public static class ATS_CompareAddresses
    {

        public class CompareAddresses_Request
        {
            public List<trentDataClass> trentData { get; set; }
            public string cjData { get; set; }
        }

        public class CompareAddresses_Response
        {
            public List<dcFileItem> dcFile { get; set; }
            public string logFile { get; set; }


            public List<string> unmatchedStaff { get; set; }

            public List<string> nonMailingFlag { get; set; }

            public bool hasData { get; set; }
        }

        public class trentDataClass
        {
            public string reference_no { get; set; }
            //public string pos_ref_no { get; set; }
            public string address_line1 { get; set; }
            public string address_line2 { get; set; }
            public string address_line6 { get; set; }

            public string mailing_address_i { get; set; }
        }


        public class localCache
        {
            public trentDataClass trentData { get; set; }


            public string add1 { get; set; }
            public string add2 { get; set; }
            public string add3 { get; set; }
            public string postcode { get; set; }
            public string country { get; set; }
        }

        public class dcFileItem
        {
            public string PER_REF_NO { get; set; }
            public string ADDRESS_TYPE { get { return "HOME"; } }
            public string ADDR_START_DATE { get { return DateTime.Now.Date.ToString("yyyyMMdd"); } }
            public string ADDR_LINE_2 { get; set; }
            public string ADDR_LINE_3 { get; set; }
            public string ADDR_LINE_4 { get; set; }
            public string ADDR_LINE_6 { get; set; }
        }



        private static bool isSame(string s1, string s2)
        {
            return !string.IsNullOrWhiteSpace(s1) && !string.IsNullOrWhiteSpace(s2) && s1.ToUpper() == s2.ToUpper();
        }


        [FunctionName("ATS_CompareAddresses")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(CompareAddresses_Request))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CompareAddresses_Response))]
        public static async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            CompareAddresses_Request request = await Helpers.BodyReader.Read<CompareAddresses_Request>(req.Body);

            if (request.trentData.Count > 0 && request.cjData.Length > 0)
            {
                var bytes = Convert.FromBase64String(request.cjData);
                using (ExcelPackage package = new ExcelPackage(new MemoryStream(bytes)))
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    if (package.Workbook.Worksheets.Count() > 0)
                    {
                        if (package.Workbook.Worksheets.Select(w => w.Name).Contains("PlacementRecords"))
                        {

                            ExcelWorksheet worksheet = package.Workbook.Worksheets["PlacementRecords"];

                            var rowCnt = worksheet.Dimension.End.Row;
                            var colCnt = worksheet.Dimension.End.Column;

                            List<string> requiredHeaders = new List<string> { "Person Ref", "Address Line 1", "Address Line 2", "Address Line 3", "Postcode", "Country", "ID" };
                            Dictionary<string, int> headerPositions = new Dictionary<string, int>();

                            var headerRow = worksheet.Cells[1, 1, 1, colCnt];
                            List<string> fileHeaders = headerRow.Select(c => c.GetValue<string>()).ToList();
                            foreach (string rHeader in requiredHeaders.ToList())
                            {
                                ExcelRangeBase x = headerRow.SingleOrDefault(c => c.GetValue<string>() == rHeader);
                                if (x != null)
                                {
                                    headerPositions.Add(rHeader, x.Start.Column);
                                }
                            }

                            List<string> StaffRefs = request.trentData.Select(t => t.reference_no).Distinct().ToList();




                            if (requiredHeaders.Count == headerPositions.Count)
                            {
                                Dictionary<string, localCache> cjWorkers = new Dictionary<string, localCache>();

                                List<string> missingWorkers = new List<string>();

                                for (var r = 2; r <= rowCnt; r++)
                                {
                                    ExcelRange row = worksheet.Cells[r, 1, r, colCnt];
                                    string StaffRef = row[r, headerPositions["Person Ref"]].GetValue<string>();

                                    if (!string.IsNullOrWhiteSpace(StaffRef))
                                    {
                                        if (!cjWorkers.ContainsKey(StaffRef))
                                        {
                                            if (StaffRefs.Contains(StaffRef))
                                            {
                                                string add1 = row[row.Start.Row, headerPositions["Address Line 1"]].GetValue<string>();
                                                string add2 = row[row.Start.Row, headerPositions["Address Line 2"]].GetValue<string>();
                                                string add3 = row[row.Start.Row, headerPositions["Address Line 3"]].GetValue<string>();
                                                string postcode = row[row.Start.Row, headerPositions["Postcode"]].GetValue<string>();
                                                string country = row[row.Start.Row, headerPositions["Country"]].GetValue<string>();

                                                cjWorkers.Add(StaffRef, new localCache { trentData = request.trentData.Single(t => t.reference_no == StaffRef), add1 = add1, add2 = add2, add3 = add3, postcode = postcode, country = country });
                                            }
                                            else
                                            {
                                                missingWorkers.Add(StaffRef);
                                            }
                                        }
                                    }
                                }

                                if (cjWorkers.Count > 0)
                                {
                                    foreach (var w in cjWorkers.ToList())
                                    {
                                        if ((isSame(w.Value.add1, w.Value.trentData.address_line1) || isSame(w.Value.add1, w.Value.trentData.address_line2) || isSame(w.Value.add1, string.Concat(w.Value.trentData.address_line2, ' ', w.Value.trentData.address_line2))) && isSame(w.Value.postcode, w.Value.trentData.address_line6))
                                        {
                                            cjWorkers.Remove(w.Key);
                                        }
                                    }

                                    if (cjWorkers.Count > 0)
                                    {
                                        StringBuilder logFile = new StringBuilder();
                                        List<dcFileItem> dcFile = new List<dcFileItem>();
                                        List<string> nonMailingFlag = new List<string>();

                                        logFile.AppendLine("trent_reference_no,trent_address_line1,trent_address_line2,trent_address_line6,trent_mailing_address_i,cj_add1,cj_add2,cj_add3,cj_postcode");

                                        foreach (var d in cjWorkers)
                                        {
                                            dcFile.Add(new dcFileItem
                                            {
                                                ADDR_LINE_2 = (d.Value.add1 ?? "").Replace(",", "■"),
                                                ADDR_LINE_3 = (d.Value.add2 ?? "").Replace(",", "■"),
                                                ADDR_LINE_4 = (d.Value.add3 ?? "").Replace(",", "■"),
                                                ADDR_LINE_6 = (d.Value.postcode ?? "").Replace(",", "■"),
                                                PER_REF_NO = d.Value.trentData.reference_no
                                            });

                                            if (d.Value.trentData.mailing_address_i != "T")
                                            {
                                                nonMailingFlag.Add(d.Key);
                                            }


                                            logFile.AppendLine($"\"{d.Value.trentData.reference_no}\",\"{d.Value.trentData.address_line1}\",\"{d.Value.trentData.address_line2}\",\"{d.Value.trentData.address_line6}\",\"{d.Value.trentData.mailing_address_i}\",\"{d.Value.add1}\",\"{d.Value.add2}\",\"{d.Value.add3}\",\"{d.Value.postcode}\"");
                                        }

                                        return new OkObjectResult(new CompareAddresses_Response { logFile = logFile.ToString(), dcFile = dcFile, unmatchedStaff = missingWorkers.Distinct().ToList(), nonMailingFlag = nonMailingFlag, hasData = true });
                                    }
                                }
                            }
                            else
                            {
                                return new BadRequestObjectResult(new { error = "Missing required headers" });
                            }

                            return new OkObjectResult(new CompareAddresses_Response { hasData = false });

                        }
                    }
                }
            }
            return new BadRequestResult();
        }
    }
}
