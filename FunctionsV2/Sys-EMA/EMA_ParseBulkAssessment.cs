using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public static class EMA_ParseBulkAssessment
{
    [FunctionName("EMA_ParseBulkAssessment")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Processing a request to transform JSON.");

        // Read request body
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        // Deserialize input JSON
        var input = JsonConvert.DeserializeObject<List<InputModel>>(requestBody);

        // Perform transformation
        var output = input.Select(mod => new OutputModel
        {
            mod_code = mod.mod_code,
            mav_occur = mod.mav_occur,
            ayr_code = mod.ayr_code,
            psl_code = mod.psl_code,
            map_code = mod.map_code,
            mab = (mod.mabs ?? new List<InputMab>()).Select(mab => new OutputMab
            {
                map_code = mod.map_code,
                mab_seq = mab.mab_seq,
                mab_type = mab.mab_type,
                mabraw = (mab.mab_xml?.mab_cams ?? new List<InputCam>()).Select(cam => new OutputCam
                {
                    map_code = cam.map_code_mab_cams,
                    //map_code = cam.map_code_mab_cams,
                    mab_seq = cam.mab_seq_mab_cams,
                    ast_code = cam.ast_code_mab_cams,
                    mab_perc = cam.mab_perc_mab_cams,
                    mab_tsha = cam.mab_tsha_mab_cams,
                    mab_name = cam.mab_name_mab_cams,
                    mks_code = cam.mks_code_mab_cams,
                    mab_agrp = cam.mab_agrp_mab_cams,
                    mab_udf1 = cam.mab_udf1_mab_cams,
                    mab_udfa = cam.mab_udfa_mab_cams,
                    mab_udfe = cam.mab_udfe_mab_cams,
                    mab_udfd = cam.mab_udfd_mab_cams,
                    mad = cam.mad?.mad_cams != null ? cam.mad.mad_cams.Select(madCam => new OutputMad
                    {
                        mod_code = madCam.mod_code_mad_cams,
                        madRaw = new OutputMadRaw
                        {
                            mod_code = madCam.mod_code_mad_cams,
                            mav_occur = madCam.mav_occur_mad_cams,
                            ayr_code = madCam.ayr_code_mad_cams,
                            psl_code = madCam.psl_code_mad_cams,
                            map_code = madCam.map_code_mad_cams,
                            mab_seq = madCam.mab_seq_mad_cams,
                            mad_ddate = madCam.mad_ddate_mad_cams,
                            mad_duet = madCam.mad_duet_mad_cams
                        }
                    }).FirstOrDefault() : null
                }).ToList()
            }).ToList()
        }).ToList();

        // Return the transformed data
        return new OkObjectResult(output);
    }

    public class InputModel
    {
        public string mod_code { get; set; }
        public string mav_occur { get; set; }
        public string ayr_code { get; set; }
        public string psl_code { get; set; }
        public string map_code { get; set; }
        public List<InputMab> mabs { get; set; }
    }

    public class InputMab
    {
        public string mab_seq { get; set; }
        public int mab_type { get; set; }
        public MabXml mab_xml { get; set; }
    }

    public class MabXml
    {
        [JsonConverter(typeof(SingleOrArrayConverter<InputCam>))]
        public List<InputCam> mab_cams { get; set; }
    }

    public class InputCam
    {
        public string map_code_mab_cams { get; set; }
        public string ast_code_mab_cams { get; set; }
        public string mab_perc_mab_cams { get; set; }
        public string mab_name_mab_cams { get; set; }

        //public string map_code_mab_cams { get; set; }
        public int mab_seq_mab_cams { get; set; }
        public string mab_tsha_mab_cams { get; set; }
        public string mks_code_mab_cams { get; set; }

        public string mab_agrp_mab_cams { get; set; }
        public string mab_udf1_mab_cams { get; set; }
        public string mab_udfa_mab_cams { get; set; }

        public string mab_udfe_mab_cams { get; set; }
        public string mab_udfd_mab_cams { get; set; }


        public InputMad mad { get; set; }
    }

    public class InputMad
    {
        public string mod_code_mad_cams { get; set; }

        [JsonConverter(typeof(SingleOrArrayConverter<MadCams>))]
        public List<MadCams> mad_cams { get; set; }
    }

    public class MadCams
    {
        public string mod_code_mad_cams { get; set; }
        public string mav_occur_mad_cams { get; set; }
        public string ayr_code_mad_cams { get; set; }
        public string psl_code_mad_cams { get; set; }
        public string map_code_mad_cams { get; set; }
        public int mab_seq_mad_cams { get; set; }
        public string mad_ddate_mad_cams { get; set; }
        public string mad_duet_mad_cams { get; set; }
    }

    // Output models
    public class OutputModel
    {
        public string mod_code { get; set; }
        public string mav_occur { get; set; }
        public string ayr_code { get; set; }
        public string psl_code { get; set; }
        public string map_code { get; set; }
        public List<OutputMab> mab { get; set; }
    }

    public class OutputMab
    {
        public string mab_seq { get; set; }
        public int mab_type { get; set; }

        public string map_code { get; set; }
        public List<OutputCam> mabraw { get; set; }
    }

    public class OutputCam
    {
        public string map_code { get; set; }
        public string ast_code { get; set; }
        public string mab_perc { get; set; }
        public string mab_name { get; set; }
        //map_code = cam.map_code_mab_cams,
        public int mab_seq { get; set; }
        //ast_code = cam.ast_code_mab_cams,
        // mab_perc = cam.mab_perc_mab_cams,
        public string mab_tsha { get; set; }
        //mab_name = cam.mab_name_mab_cams,
        public string mks_code { get; set; }
        public string mab_agrp { get; set; }
        public string mab_udf1 { get; set; }
        public string mab_udfa { get; set; }
        public string mab_udfe { get; set; }
        public string mab_udfd { get; set; }
        public OutputMad mad { get; set; }
    }

    public class OutputMad
    {
        public string mod_code { get; set; }

        public OutputMadRaw madRaw { get; set; }

    }

    public class OutputMadRaw
    {
        public string mod_code { get; set; }
        public string map_code { get; set; }
        public int mab_seq { get; set; }
        public string mav_occur { get; set; }
        public string ayr_code { get; set; }
        public string psl_code { get; set; }
        public string mad_ddate { get; set; }
        public string mad_duet { get; set; }
    }
    public class SingleOrArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(List<T>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = Newtonsoft.Json.Linq.JToken.Load(reader);

            // If it's a single object, wrap it in a list
            if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object)
            {
                return new List<T> { token.ToObject<T>() };
            }

            // If it's already an array, deserialize as usual
            return token.ToObject<List<T>>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
