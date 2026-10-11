using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public static class EMA_ParseBulkImport
{
    [FunctionName("EMA_ParseBulkImport")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        // Read the request body
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        // Replace '.' with '_'
        requestBody = requestBody.Replace('.', '_');
        string norm_reqBody = requestBody.Contains("\\n") ? requestBody.Replace("\\n", " ") : requestBody;

        // Deserialize the modified JSON
        var inputModel = JsonConvert.DeserializeObject<InputModel>(norm_reqBody);

        // Check if the input model is valid
        if (inputModel?.risis_ema_mav_asses?.mav?.mav_cams == null)
        {
            return new BadRequestObjectResult("Invalid input structure.");
        }

        var outputModel = new OutputModel
        {
            mav = new List<MavOutput>()
        };

        foreach (var mavItem1 in inputModel.risis_ema_mav_asses.mav.mav_cams)
        {
            if (mavItem1 == null) continue; // Skip null items

            var mavOutput = new MavOutput
            {
                mod_code = mavItem1.mod_code_mav_cams,
                mav_occur = mavItem1.mav_occur_mav_cams,
                ayr_code = mavItem1.ayr_code_mav_cams,
                psl_code = mavItem1.psl_code_mav_cams,
                map_code = mavItem1.map_code_mav_cams,
                mab = new List<MabOutputItem>() // Changed to initialize as a List
            };

            if (mavItem1.mab?.mab_cams == null) continue; // Check if mab_cams exists

            foreach (var mabItem1 in mavItem1.mab.mab_cams)
            {
                if (mabItem1 == null) continue; // Skip null items

                var mabOutputItem = new MabOutputItem
                {
                    map_code = mabItem1.map_code_mab_cams,
                    mab_seq = mabItem1.mab_seq_mab_cams,
                    ast_code = mabItem1.ast_code_mab_cams,
                    mab_perc = mabItem1.mab_perc_mab_cams,
                    mab_tsha = mabItem1.mab_tsha_mab_cams,
                    mab_name = mabItem1.mab_name_mab_cams,
                    mks_code = mabItem1.mks_code_mab_cams,
                    mab_agrp = mabItem1.mab_agrp_mab_cams,
                    mab_rasi = mabItem1.mab_rasi_mab_cams,
                    mab_udf1 = mabItem1.mab_udf1_mab_cams,
                    mab_udfa = mabItem1.mab_udfa_mab_cams,
                    mab_udfb = mabItem1.mab_udfb_mab_cams,
                    mab_udfc = mabItem1.mab_udfc_mab_cams,
                    mab_udfd = mabItem1.mab_udfd_mab_cams,
                    mab_udfe = mabItem1.mab_udfe_mab_cams,
                    mad = new List<MadOutputItem>() // Initialize the mad list
                };

                if (mabItem1.mad?.mad_cams != null)
                {
                    foreach (var madItem in mabItem1.mad.mad_cams)
                    {
                        if (madItem == null) continue; // Skip null items

                        var madOutputItem = new MadOutputItem
                        {
                            mod_code = madItem.mod_code_mad_cams,
                            mav_occur = madItem.mav_occur_mad_cams,
                            ayr_code = madItem.ayr_code_mad_cams,
                            psl_code = madItem.psl_code_mad_cams,
                            map_code = madItem.map_code_mad_cams,
                            mab_seq = madItem.mab_seq_mad_cams,
                            mad_ddate = madItem.mad_ddate_mad_cams,
                            mad_duet = madItem.mad_duet_mad_cams
                        };

                        // Add mad items directly to mabOutputItem
                        mabOutputItem.mad.Add(madOutputItem);
                    }
                }

                // Add the mabOutputItem to the list of mab items
                mavOutput.mab.Add(mabOutputItem);
            }

            // Add the mavOutput to the overall output model
            outputModel.mav.Add(mavOutput);
        }

        return new OkObjectResult(outputModel);
    }
}

public class InputModel
{
    public risisEmaMavAsses risis_ema_mav_asses { get; set; }
}

public class risisEmaMavAsses
{
    public Mav1 mav { get; set; }
}

public class Mav1
{
    public List<MavCams> mav_cams { get; set; }
}

public class MavCams
{
    public string mod_code_mav_cams { get; set; }
    public string mav_occur_mav_cams { get; set; }
    public string ayr_code_mav_cams { get; set; }
    public string psl_code_mav_cams { get; set; }
    public string map_code_mav_cams { get; set; }
    public Mab1 mab { get; set; }
}

public class Mab1
{
    public List<MabCams> mab_cams { get; set; }
}

public class MabCams
{
    public string map_code_mab_cams { get; set; }
    public string mab_seq_mab_cams { get; set; }
    public string ast_code_mab_cams { get; set; }
    public string mab_perc_mab_cams { get; set; }
    public string mab_tsha_mab_cams { get; set; }
    public string mab_name_mab_cams { get; set; }
    public string mks_code_mab_cams { get; set; }
    public string mab_agrp_mab_cams { get; set; }
    public string mab_rasi_mab_cams { get; set; }
    public string mab_udf1_mab_cams { get; set; }
    public string mab_udfa_mab_cams { get; set; }
    public string mab_udfb_mab_cams { get; set; }
    public string mab_udfc_mab_cams { get; set; }
    public string mab_udfd_mab_cams { get; set; }
    public string mab_udfe_mab_cams { get; set; }
    public Mad1 mad { get; set; }
}

public class Mad1
{
    public List<MadCams> mad_cams { get; set; }
}

public class MadCams
{
    public string mod_code_mad_cams { get; set; }
    public string mav_occur_mad_cams { get; set; }
    public string ayr_code_mad_cams { get; set; }
    public string psl_code_mad_cams { get; set; }
    public string map_code_mad_cams { get; set; }
    public string mab_seq_mad_cams { get; set; }
    public string mad_ddate_mad_cams { get; set; }
    public string mad_duet_mad_cams { get; set; }
}
public class OutputModel
{
    public List<MavOutput> mav { get; set; }
}

public class MavOutput
{
    public string mod_code { get; set; }
    public string mav_occur { get; set; }
    public string ayr_code { get; set; }
    public string psl_code { get; set; }
    public string map_code { get; set; }
    public List<MabOutputItem> mab { get; set; } // Changed to a list directly
}

public class MabOutputItem
{
    public string map_code { get; set; }
    public string mab_seq { get; set; }
    public string ast_code { get; set; }
    public string mab_perc { get; set; }
    public string mab_tsha { get; set; }
    public string mab_name { get; set; }
    public string mks_code { get; set; }
    public string mab_agrp { get; set; }
    public string mab_rasi { get; set; }
    public string mab_udf1 { get; set; }
    public string mab_udfa { get; set; }
    public string mab_udfb { get; set; }
    public string mab_udfc { get; set; }
    public string mab_udfd { get; set; }
    public string mab_udfe { get; set; }
    public List<MadOutputItem> mad { get; set; } // Keep this as a list for mad items
}

public class MadOutputItem
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
