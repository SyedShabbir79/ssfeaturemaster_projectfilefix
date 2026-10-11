using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using FunctionsV2.Sys_FeesCheck.Models;
using System.Text.RegularExpressions;

namespace FunctionsV2.Sys_FeesCheck {
  public class CheckFees {
		#region OpenAPI Attributes
		[FunctionName("FeesCheck_CheckFees")]
    [OpenApiOperation]
    [OpenApiSecurity(
      "function_key",
      SecuritySchemeType.ApiKey,
      Name = "code",
      In = OpenApiSecurityLocationType.Query
    )]
    [OpenApiRequestBody(
      contentType: "application/json",
      bodyType: typeof(string)
    )]
    [OpenApiResponseWithBody(
      statusCode: HttpStatusCode.OK,
      contentType: "application/json",
      bodyType: typeof(OutputDataForSITS[]),
      Description = "A list of student IDs, join codes, SEQ2s and the student's balance (if it is less than £0.01)"
    )]
		#endregion
		public static async Task<IActionResult> Run(
      [HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)]
      HttpRequest req,
      ILogger log
    ) {
      #region Initialize variables
			bool printDebugLog = req.Query.ContainsKey("EnableDebugLog");
      bool printOutputLog = req.Query.ContainsKey("EnableOutputLog");
      string body;
      FeesRawData data;
      List<AgressoRawData> aggregatedAgressoData = new();
      List<SITSCleanData> sitsCleanData = new();
      List<OutputDataForSITS> output = new();
      #endregion

      #region Extract the body
      try {
        if (printDebugLog) log.LogInformation("Extracting body from request...");
        body = await new StreamReader(req.Body).ReadToEndAsync();
        if (printDebugLog) log.LogInformation("Extracted body from request.");
      } catch (Exception ex) {
        string errorMessage = "Error when reading body from request";
        log.LogError($"{errorMessage}!");
        log.LogError($"Exception: {ex}");
        return new BadRequestObjectResult($"{errorMessage}: {ex}");
      }
			#endregion

			#region Ensure that the body is not empty
			if (string.IsNullOrEmpty(body)) {
        string errorMessage = "Empty POST body provided!";
        log.LogError(errorMessage);
        return new BadRequestObjectResult(errorMessage);
      }
			#endregion

			#region Deserialize the data
			try {
        if (printDebugLog) log.LogInformation("Deserializing body as OutstandingFeesRawData object...");
        data = JsonConvert.DeserializeObject<FeesRawData>(body);
        if (printDebugLog) log.LogInformation("Deserialized body as OutstandingFeesRawData object.");
      } catch (Exception ex) {
        string errorMessage = "Error when deserializing body into OutstandingFeesRawData object";
        log.LogError($"{errorMessage}!");
        log.LogError($"Exception: {ex}");
        return new BadRequestObjectResult($"{errorMessage}: {ex}");
      }
      #endregion

      #region Ensure that the data provided is not empty
      if (data.agressoRawData == null) {
        string errorMessage = "No agressoRawData provided!";
        log.LogError(errorMessage);
        return new BadRequestObjectResult(errorMessage);
      }
      if (data.sitsRawData == null) {
        string errorMessage = "No sitsRawData provided!";
        log.LogError(errorMessage);
        return new BadRequestObjectResult(errorMessage);
      }
      #endregion

      #region Aggregate agresso data (sum all of user's paid amounts)
      foreach (var item in data.agressoRawData) {
        var existingItem = aggregatedAgressoData.Find(i => i.student_id == item.student_id);
        // If item doesn't exist in aggregated list yet, increment
        if (existingItem != null) existingItem.paid += item.paid;
        // Otherise, add it to the list with it's current amount
        else aggregatedAgressoData.Add(item);
    	}
      #endregion

      #region Clean up SITS data
      // Remove nulls
      var validSITSData = data.sitsRawData.Where(i => i.sce_stuc != null);
      // Convert to "clean" data (use Regex to parse amount owed from a string to a decimal)
      foreach (var item in validSITSData) sitsCleanData.Add(new SITSCleanData(item));
      #endregion

      #region Select student IDs from both datasets (to form intersect)
      // Select all student_ids in the Agresso dataset
      List<string> agressoStudentIDs = aggregatedAgressoData.Select(i => i.student_id).ToList();
      if (printDebugLog) log.LogInformation($"Found {agressoStudentIDs.Count} items in Agresso");

      // Select all student_ids in the SITS dataset (via sce_stuc)
      List<string> sitsStudentIDs = sitsCleanData.Select(i => i.stuc).ToList();
      if (printDebugLog) log.LogInformation($"Found {sitsStudentIDs.Count} items in SITS");

      // Get a list of all of the student_ids in both datasets
      List<string> studentIDs = agressoStudentIDs.Intersect(sitsStudentIDs).ToList();
      if (printDebugLog) log.LogInformation($"Found {studentIDs.Count} items in both datasets");
			#endregion

			#region Calculate students' unpaid balance, and add them to the output if it's less than £0.01
			foreach (string id in studentIDs) {
				#region Ensure that student exists in both datasets
				if (!aggregatedAgressoData.Any(i => i.student_id == id)) {
          if (printDebugLog) log.LogWarning($"{id} could not be found in the Agresso dataset.");
          continue;
        }
        if (!data.sitsRawData.Any(i => i.sce_stuc == id)) {
          if (printDebugLog) log.LogWarning($"{id} could not be found in the SITS dataset.");
          continue;
        }
				#endregion

				AgressoRawData agressoUser = aggregatedAgressoData.Find(i => i.student_id == id);
        SITSCleanData sitsUser = sitsCleanData.Find(i => i.stuc == id);

        decimal balance = sitsUser.unpaidFees - agressoUser.paid;
        if (printDebugLog) log.LogInformation($"{id} has a balance of {balance}");

        // If the student's total balance is greater than £0.01, skip over them
        bool hasPaid = balance > (decimal)0.01;
        if (printDebugLog) log.LogInformation($"{id} {(hasPaid ? "has" : "has NOT")} yet paid all of their fees (unpaid fees: {sitsUser.unpaidFees}, paid fees: {agressoUser.paid}, balance: {balance})");
        if (hasPaid) continue;

        if (!printDebugLog && printOutputLog) log.LogInformation($"{id} has a balance of {balance}, which is less than or equal to 0.01. Adding them to the output array.");
        OutputDataForSITS outUser = new(id, sitsUser.scjc, sitsUser.seq2, balance);
        output.Add(outUser);
      }
			#endregion

			return new OkObjectResult(output);
    }
  }
}