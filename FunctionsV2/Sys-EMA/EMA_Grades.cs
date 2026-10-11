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
using static FunctionsV2.Sys_EMA.EMA_Classes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

/// <summary>
/// Duncan Taylor - 10/02/2023
/// Helper methods to expand optional data string from Bb GradesJourney outgoing payload and reformat as JSON map
/// </summary>

namespace FunctionsV2.Sys_EMA
{

    public static class EMA_Grades
    {

        [FunctionName("EMA_ExpandGrades")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(GradesRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(GradesResponse))]
        public static async Task<IActionResult> ExpandGrades([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function EMA_ExpandGrades invoked");

            // get data from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrWhiteSpace(requestBody))
                return new BadRequestObjectResult(error: new GradesResponse { error = "Missing request body", hasError = true });

            // deserialize request body
            GradesRequest gradesRequest = JsonConvert.DeserializeObject<GradesRequest>(requestBody);
            //log.LogInformation(JsonConvert.SerializeObject(gradesRequest, Formatting.Indented));

            // initialise a list of grades to pass back
            List<GradeOut> grades = new List<GradeOut>();

            // loop through grades and parse optionalData
            foreach (GradeIn grade in gradesRequest.grades)
            {
                IDictionary<string, string> data = new Dictionary<string, string>();
                string[] optionalDataArray = grade.optionalDataIn.Split(":");

                for (int i = 0; i < optionalDataArray.Length; i++)
                {
                    var kvPair = optionalDataArray[i].Split("=");
                    data.Add(kvPair[0], kvPair[1]);
                }
                // serialise Dictionary and then decerialise again as OptionalData object 
                OptionalData optionalData = JsonConvert.DeserializeObject<OptionalData>(JsonConvert.SerializeObject(data));

                // initialise a grade to pass back 
                GradeOut gradeOut = new GradeOut();
                // pass other fields straight back with the addition of an expanded JSON optionalData set as well as the original string               
                gradeOut.stuCode = grade.stuCode;
                gradeOut.courseId = grade.courseId;
                gradeOut.columnId = grade.columnId;
                gradeOut.pointsPossible = grade.pointsPossible;
                gradeOut.displayGrade = grade.displayGrade;
                gradeOut.percentageMark = grade.percentageMark;
                gradeOut.optionalDataString = grade.optionalDataIn;
                gradeOut.optionalDataOut = optionalData;
                // add grade to list for return
                grades.Add(gradeOut);
            }

            // initialise objects to return
            GradesResponse gradesResponse = new GradesResponse();
            gradesResponse.grades = grades;
            gradesResponse.footer = gradesRequest.footer;
            gradesResponse.hasError = false;

            // return updated payload as a 200 response
            return new OkObjectResult(gradesResponse);

        }

    }

}