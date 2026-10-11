using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static FunctionsV2.Sys_HRMS.HRMS_Location;
using static FunctionsV2.Sys_HRMS.HRMS_PayCategory;
using static FunctionsV2.Sys_HRMS.HRMS_StaffDetails;
using static FunctionsV2.Sys_HRMS.HRMS_OverseasStaff;
using static FunctionsV2.Sys_HRMS.HRMS_LMSTimeZone;
using System.ComponentModel;
using RestSharp.Extensions;


/// <summary>
/// Duncan Taylor - 22/08/2023
/// Azure Function methods
/// </summary>
/// 


namespace FunctionsV2.Sys_HRMS
{
    public static class HRMS_Functions
    {

        [FunctionName("HRMS_Calculate_Location")]
        [OpenApiOperation]
        [OpenApiParameter("locationCode", Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(StaffLocation))]
        public static IActionResult CalcLocation([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function HRMS_Calculate_Location invoked");

            // get query argument location code
            string locCode = req.Query["locationCode"];

            log.LogInformation($"locationCode: {locCode}");

            StaffLocation locationResponse = new StaffLocation(locCode);

            // check that we have values for location
            if (String.IsNullOrWhiteSpace(locationResponse.location) || locationResponse.hasError)
            {
                log.LogError("No location found.");
                return new BadRequestObjectResult(locationResponse);
            }

            // Return batch containing decrypted fields
            return new OkObjectResult(locationResponse);
        }


        [FunctionName("HRMS_Calculate_PayCategory")]
        [OpenApiOperation]
        [OpenApiParameter("employeeGroup", Required = true, Type = typeof(string))]
        [OpenApiParameter("employeeSubgroup", Required = true, Type = typeof(string))]
        [OpenApiParameter("paygrade", Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(StaffPayCategory))]
        public static IActionResult CalcPayCategory([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function HRMS_Calculate_PayCategory invoked");

            // get query arguments
            string empGrp = req.Query["employeeGroup"];
            string empSubGrp = req.Query["employeeSubgroup"];
            string paygrade = req.Query["paygrade"];
            log.LogInformation($"employeeGroup: {empGrp}, employeeSubgroup: {empSubGrp}, paygrade: {paygrade}.");

            StaffPayCategory payCategoryResponse = new StaffPayCategory(empGrp, empSubGrp, paygrade);

            // check that we have something to return
            if (String.IsNullOrEmpty(payCategoryResponse.payCategory) || payCategoryResponse.hasError)
            {
                log.LogError("No pay category found.");
                return new BadRequestObjectResult(payCategoryResponse);
            }

            // Return payCategoryResponses
            return new OkObjectResult(payCategoryResponse);
        }

        [FunctionName("HRMS_Calculate_OverseasStaff")]
        [OpenApiOperation]
        [OpenApiParameter("school", Type = typeof(string))]
        [OpenApiParameter("location", Required = true, Type = typeof(string))]
        [OpenApiParameter("on_payroll", Required = true, Type = typeof(Boolean))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(OverseasStaff))]
        public static IActionResult CalcOverseasStaff([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function HRMS_Calculate_OverseasStaff invoked");

            // get query arguments
            string schoolName = req.Query["school"];
            string location = req.Query["location"];
            string onPayrollString = req.Query["on_payroll"];
            bool onPayroll = bool.TryParse(onPayrollString, out bool onPay);
            log.LogInformation($"school: {schoolName}, on_payroll: {onPay}, location: {location}.");

            OverseasStaff OverseasStaffResponse = new OverseasStaff(schoolName, onPay, location);

            // check that we have something to return
            if (String.IsNullOrEmpty(OverseasStaffResponse.staffCode) || OverseasStaffResponse.hasError)
            {
                log.LogError("No staff code found.");
                return new BadRequestObjectResult(OverseasStaffResponse);
            }

            // Return payCategoryResponses
            return new OkObjectResult(OverseasStaffResponse);
        }

        [FunctionName("HRMS_Calculate_LMSTimeZone")]
        [OpenApiOperation]
        [OpenApiParameter("overseasStaff", Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(OverseasStaff))]
        public static IActionResult CalcLMSTimeZone([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function HRMS_Calculate_LMSTimeZone invoked");

            // get query arguments
            string overseasStaff = req.Query["overseas_sfaff"];
            log.LogInformation($"overseas_staff: {overseasStaff}.");

            LMSTimeZone LMSTimeZoneResponse = new LMSTimeZone(overseasStaff);

            // check that we have something to return
            if (String.IsNullOrEmpty(LMSTimeZoneResponse.timeZone) || LMSTimeZoneResponse.hasError)
            {
                log.LogError("No LMS Timezone found.");
                return new BadRequestObjectResult(LMSTimeZoneResponse);
            }

            // Return payCategoryResponses
            return new OkObjectResult(LMSTimeZoneResponse);
        }


        [FunctionName("HRMS_Calculate_StaffDetails")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(StaffDetailsRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(StaffDetails))]
        public static async Task<IActionResult> CalcStaffDetails([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function HRMS_Calculate_StaffDetails invoked");

            // get request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrWhiteSpace(requestBody))
            {
                log.LogError("Missing request body");
                return new BadRequestObjectResult(error: new StaffDetails(null, null, null, true, "Missing request body"));
            }

            // deserialize request body
            StaffDetailsRequest request = JsonConvert.DeserializeObject<StaffDetailsRequest>(requestBody);
            log.LogInformation($"employeeGroup: {request.employeeGroup}, employeeSubgroup: {request.employeeSubgroup}, paygrade: {request.paygrade}, locationCode: {request.locationCode}.");

            // calculate required staff details by instantiating custom classes using input data from request body 
            StaffLocation locationResponse = new StaffLocation(request.locationCode);
            StaffPayCategory payCategoryResponse = new StaffPayCategory(request.employeeGroup, request.employeeSubgroup, request.paygrade);
            OverseasStaff overseasStaffResponse = new OverseasStaff(request.school, request.on_payroll, request.locationCode);

            // initialise response object
            StaffDetails staffDetails = new StaffDetails(locationResponse.location, payCategoryResponse.payCategory, overseasStaffResponse.staffCode, (locationResponse.hasError || payCategoryResponse.hasError));

            if (staffDetails.hasError)
            {
                log.LogError($"Error in sub-function");
                staffDetails.error = $"Location error: {locationResponse.error}. PayCategory error: {payCategoryResponse.error}";
                return new BadRequestObjectResult(staffDetails);
            }
            
            if (String.IsNullOrEmpty(staffDetails.location) || String.IsNullOrEmpty(staffDetails.payCategory) || String.IsNullOrEmpty(staffDetails.overseasStaff))
            {
                log.LogError("Missing detail value");
                staffDetails.hasError = true;
                staffDetails.error = $"Missing one or more return value. Location: {locationResponse.location}. PayCategory: {payCategoryResponse.payCategory}. OverseasStaff: {overseasStaffResponse.staffCode}";
                return new BadRequestObjectResult(staffDetails);
            }
            
            // return staff details
            log.LogInformation($"HRMS_Calculate_StaffDetails returning location: {staffDetails.location} and paycategory {staffDetails.payCategory}.");
            return new OkObjectResult(staffDetails);
        }
    }
}
