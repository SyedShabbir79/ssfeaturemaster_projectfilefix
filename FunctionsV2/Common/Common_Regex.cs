using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace FunctionsV2.Common
{
    public class IsMatchRequest
    {
        public string name { get; set; }
        public string pattern { get; set; }
        public string input { get; set; }

        public string customMessage { get; set; }

        internal bool isMatch()
        {
            return Regex.IsMatch(input, pattern);
        }
    }

    public class IsMatchesRequest
    {
        public List<IsMatchRequest> Requests { get; set; }
    }

    public class IsMatchResponse : IsMatchRequest
    {
        public string error { get; set; }
    }



    public class IsMatchesResponseRegex
    {
        public List<IsMatchResponse> errors { get; set; }
        public bool hasErrors { get; set; }
    }


    public class RegexReplaceRequest
    {
        public IList<RegexReplaceField> fieldList { get; set; }
    }

    public class RegexReplaceField
    {
        public string fieldName { get; set; }
        public string fieldValue { get; set; }
        public string regexExpression { get; set; }
        public string regexReplacement { get; set; }
    }

    public class RegexReplaceResponse : RegexReplaceRequest
    {
        public string error { get; set; }
        public bool hasError { get; set; }
    }



    public static class Common_Regex
    {
        [FunctionName("Common_Regex_IsMatch")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(IsMatchesRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(IsMatchesResponseRegex))]
        public static async Task<HttpResponseMessage> IsMatch([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            IsMatchesRequest isMatches = Newtonsoft.Json.JsonConvert.DeserializeObject<IsMatchesRequest>(body);

            List<IsMatchResponse> errors = new List<IsMatchResponse>();
            bool hasErrors = false;

            foreach (IsMatchRequest r in isMatches.Requests)
            {
                if (!r.isMatch())
                {
                    IsMatchResponse _error = new IsMatchResponse()
                    {
                        name = r.name,
                        input = r.input,
                        pattern = r.pattern,
                        error = String.Format("Error with property {0}, value '{1}' does not match the required pattern '{2}'.", r.name, r.input, r.pattern)
                    };
                    if (!string.IsNullOrWhiteSpace(r.customMessage))
                    {
                        _error.error = String.Format(r.customMessage, r.name, r.input, r.pattern);
                        _error.pattern = null;
                        _error.input = null;

                    }
                    _error.customMessage = null;
                    errors.Add(_error);
                    hasErrors = true;
                }
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new IsMatchesResponseRegex() { hasErrors = hasErrors, errors = errors }, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }), System.Text.Encoding.UTF8, "application/json")
            };
        }


        [FunctionName("Common_Regex_BatchReplace")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(RegexReplaceRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(RegexReplaceResponse))]
        public static async Task<IActionResult> RegexBatchReplace([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function Common_Regex_BatchReplace invoked");

            // get data from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrWhiteSpace(requestBody))
                return new BadRequestObjectResult(new RegexReplaceResponse { error = "Missing request body", hasError = true });

            // Deserialize body as bacth object
            RegexReplaceRequest replaceRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<RegexReplaceRequest>(requestBody);

            // Initialise response object
            RegexReplaceResponse replaceResponse = new RegexReplaceResponse();
            // Initialise hasError status
            replaceResponse.hasError = false;
            // Initialise a List of field objects to return
            List<RegexReplaceField> responseFields = new();
            List<String> errors = new();

            // Loop through all the fields in the fieldList from the incoming batch. Each field should contain fieldName, fieldValue and regexExpression
            foreach (RegexReplaceField field in replaceRequest.fieldList)
            {

                if (String.IsNullOrEmpty(field.fieldName) || String.IsNullOrEmpty(field.regexExpression) || 
                    String.IsNullOrEmpty(field.regexReplacement))
                {
                    replaceResponse.hasError = true;
                    errors.Add($"Missing arguments, fieldName: [{field.fieldName}], " +
                        $"regexExpression: [{field.regexExpression}], regexReplacement: [{field.regexReplacement}]");
                    continue;
                }
                else
                {
                    // Initilise a new field object to pass back the updated value
                    RegexReplaceField resultField = new RegexReplaceField();
                    // Add the field name to the new field object
                    resultField.fieldName = field.fieldName;
                    
                    // If supplied fieldValue is null set resultField to null.
                    // Else transform supplied fieldValue using regex expression and return as resultField.
                    if (String.IsNullOrEmpty(field.fieldValue))
                    {
                        resultField.fieldValue = null;
                    }
                    else
                    {
                        resultField.fieldValue = Regex.Replace(field.fieldValue, field.regexExpression, field.regexReplacement);
                    }
   
                    responseFields.Add(resultField);

                }

                if (responseFields.Count == 0)
                {
                    replaceResponse.hasError = true;
                    errors.Add("No fields updated");
                }

            }
            // Return batch containing updated fields
            replaceResponse.fieldList = responseFields;
            replaceResponse.error = String.Join("; ", errors);
            return new OkObjectResult(replaceResponse);

        }

    }
}
