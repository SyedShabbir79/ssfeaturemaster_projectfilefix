using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public static class EMA_ParseCAM32Data
{
    [FunctionName("EMA_ParseCAM32Data")]
    public static async Task<IActionResult> ParseCAM32Data(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Processing XML data.");

        // Read the XML data from the request body
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return new BadRequestObjectResult("No XML data provided.");
        }

        // Parse the XML and extract data
        var parsedData = ParseCAM32Outdata(requestBody);

        // Return the parsed data as JSON
        return new OkObjectResult(parsedData);
    }

    private static List<CAM32Response> ParseCAM32Outdata(string data)
    {
        var resultCollection = new List<CAM32Response>();

        if (!string.IsNullOrWhiteSpace(data))
        {
            string pattern = @"^.*(?<spr_code>\d{8}\/\d+)\,(?<ayr_code>\d{4}\/\d)\,(?<psl_code>[\w]*)\,(?<mod_code>[\w]*)\,(?<mav_occ>[\w]*)\,((?<map_code>[\w\-,]*)\,)?(?<mab_seq>\d*).*$";
            Regex regexMatch = new Regex(pattern, RegexOptions.Multiline);
            MatchCollection matches = regexMatch.Matches(data);

            if (matches.Count > 0)
            {
                foreach (Match x in matches)
                {
                    resultCollection.Add(new CAM32Response(
                        x.Groups["spr_code"].Value,
                        x.Groups["ayr_code"].Value,
                        x.Groups["psl_code"].Value,
                        x.Groups["mod_code"].Value,
                        x.Groups["mav_occ"].Value,
                        x.Groups["map_code"].Value,
                        x.Groups["mab_seq"].Value,
                        x.Value.Trim()
                    ));
                }
            }
        }
        return resultCollection;
    }
}

public class CAM32Response
{
    public string SprCode { get; set; }
    public string AyrCode { get; set; }
    public string PslCode { get; set; }
    public string ModCode { get; set; }
    public string MavOcc { get; set; }
    public string MapCode { get; set; }
    public string MabSeq { get; set; }
    public string Response { get; set; }

    public CAM32Response(string sprCode, string ayrCode, string pslCode, string modCode, string mavOcc, string mapCode, string mabSeq, string response)
    {
        SprCode = sprCode;
        AyrCode = ayrCode;
        PslCode = pslCode;
        ModCode = modCode;
        MavOcc = mavOcc;
        MapCode = mapCode;
        MabSeq = mabSeq;
        Response = response;
    }
}
