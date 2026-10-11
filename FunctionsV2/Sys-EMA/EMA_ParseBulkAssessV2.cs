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
using System.Linq;

public static class EMA_ParseBulkAssessV2
{
    [FunctionName("EMA_ParseBulkAssessV2")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        // Deserialize input data
        var inputData = JsonConvert.DeserializeObject<List<InputData>>(requestBody);

        // Transform the data
        var transformedData = new
        {
            outdata = new
            {
                mav = inputData?.Select(item => new
                {
                    item.mod_code,
                    item.mav_occur,
                    item.ayr_code,
                    item.psl_code,
                    item.map_code,
                    mab = item.mabs?.Select(mabItem => new
                    {
                        mabItem.mab_seq,
                        mabItem.mab_type,
                        map_code = mabItem.mab_xml?.map_code,
                        ast_code = mabItem.mab_xml?.ast_code,
                        mab_perc = mabItem.mab_xml?.mab_perc,
                        mab_tsha = mabItem.mab_xml?.mab_tsha,
                        mab_name = mabItem.mab_xml?.mab_name,
                        mks_code = mabItem.mab_xml?.mks_code,
                        mab_agrp = mabItem.mab_xml?.mab_agrp,
                        mab_udf1 = mabItem.mab_xml?.mab_udf1,
                        mab_udfa = mabItem.mab_xml?.mab_udfa,
                        mab_udfd = mabItem.mab_xml?.mab_udfd,
                        mab_udfe = mabItem.mab_xml?.mab_udfe,

                        // MAD data within MAB
                        mads = mabItem.mad?.Select(madItem => new
                        {
                            mod_code = madItem.mad_xml?.mod_code,
                            mav_occur = madItem.mad_xml?.mav_occur,
                            ayr_code = madItem.mad_xml?.ayr_code,
                            psl_code = madItem.mad_xml?.psl_code,
                            map_code = madItem.mad_xml?.map_code,
                            mab_seq = madItem.mad_xml?.mab_seq,
                            mad_ddate = madItem.mad_xml?.mad_ddate,
                            mad_duet = madItem.mad_xml?.mad_duet
                        }).ToArray() ?? Array.Empty<object>() // Assign empty array if mad is null
                    }).ToArray() ?? Array.Empty<object>() // Assign empty array if mabs is null
                }).ToArray() ?? Array.Empty<object>() // Assign empty array if inputData is null
            }
        };

        return new OkObjectResult(transformedData);
    }

    // Define input data structure
    public class InputData
    {
        public string mod_code { get; set; }
        public string mav_occur { get; set; }
        public string ayr_code { get; set; }
        public string psl_code { get; set; }
        public string map_code { get; set; }
        public List<Mabs> mabs { get; set; }
    }

    public class Mabs
    {
        public string mab_seq { get; set; }
        public int mab_type { get; set; }
        public MabXml mab_xml { get; set; }
        public List<Mad> mad { get; set; }
    }

    public class MabXml
    {
        public string map_code { get; set; }
        public string mab_seq { get; set; }
        public string ast_code { get; set; }
        public string mab_perc { get; set; }
        public string mab_tsha { get; set; }
        public string mab_name { get; set; }
        public string mks_code { get; set; }
        public string mab_agrp { get; set; }
        public string mab_udf1 { get; set; }
        public string mab_udfa { get; set; }
        public string mab_udfd { get; set; }
        public string mab_udfe { get; set; }
    }

    public class Mad
    {
        public MadXml mad_xml { get; set; }
    }

    public class MadXml
    {
        public string mod_code { get; set; }
        public string mav_occur { get; set; }
        public string ayr_code { get; set; }
        public string psl_code { get; set; }
        public string map_code { get; set; }
        public string mab_seq { get; set; }
        public string mad_ddate { get; set; }
        public string mad_duet { get; set; }
    }
}
