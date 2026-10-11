using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using FunctionsV2.Helpers;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace FunctionsV2.Common
{
    public class Common_AES
    {
        private readonly IKeyVault _kv;
        public Common_AES(IKeyVault kv)
        {
            this._kv = kv;
        }


        private class AESDecryptResponse
        {
            public string plainText { get; set; }
            public string error { get; set; }
            public bool hasError { get; set; }
        }

        private class AESSitsDecryptBatch
        { 
            public IList<AESSitsDecryptField> fieldList { get; set; }
            public string error { get; set; }
            public bool hasError { get; set; }
        }

        private class AESSitsDecryptField
        {
            public string fieldName { get; set; }
            public string secretName { get; set; }
            public string fieldValue { get; set; }
        }

        [FunctionName("Common_AES_Decrypt")]
        [OpenApiOperation]
        [OpenApiParameter("secretName", Required = true, Type = typeof(string))]
        [OpenApiParameter("encryptedText", Required = true, Type = typeof(string))]

        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(AESDecryptResponse))]
        public async Task<IActionResult> AESDecrypt([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function AESDecrypt invoked");

            string secretName = req.Query["secretName"];
            string encryptedText = req.Query["encryptedText"];

            if (String.IsNullOrWhiteSpace(secretName))
                return new BadRequestObjectResult(new AESDecryptResponse { error = "Missing secretName", hasError = true });

            if (String.IsNullOrWhiteSpace(encryptedText))
                return new BadRequestObjectResult(new AESDecryptResponse { error = "Missing encryptedText", hasError = true });


            string secretValue = await _kv.GetSecret(secretName);

            if (String.IsNullOrWhiteSpace(secretValue))
                return new BadRequestObjectResult(new AESDecryptResponse { error = "Unable to get secret from KV", hasError = true });

            return new OkObjectResult(new AESDecryptResponse { plainText = DoAESDecrypt(secretValue, encryptedText), hasError = false });
        }


        [FunctionName("Common_AES_SITS_Batch_Decrypt")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(IsMatchesRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(AESSitsDecryptBatch))]
        public async Task<IActionResult> AESSitsBatchDecrypt([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function Common_AES_SITS_Batch_Decrypt invoked");

            // get data from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrWhiteSpace(requestBody))
                return new BadRequestObjectResult(new AESDecryptResponse { error = "Missing request body", hasError = true });
   
            // Deserialize body as bacth object
            AESSitsDecryptBatch batch = JsonConvert.DeserializeObject<AESSitsDecryptBatch>(requestBody);

            // Initialise an empty AESSitsDecryptBatch to populate and pass back in the response
            AESSitsDecryptBatch responseBatch = new AESSitsDecryptBatch();
            // Initialise hasError status
            responseBatch.hasError = false;
            // Initialise a List of field objects
            IList<AESSitsDecryptField> responseSets = new List<AESSitsDecryptField>();

            // Loop through all the fields in the fieldList from the incoming batch. Each field should contain fieldName, secretName and fieldValue
            foreach (AESSitsDecryptField field in batch.fieldList)
            {

                if (String.IsNullOrEmpty(field.fieldName) || String.IsNullOrEmpty(field.secretName) || String.IsNullOrEmpty(field.fieldValue))
                {
                    responseBatch.hasError = true;
                    responseBatch.error = $"Missing arguments, fieldName: {field.fieldName}, secretName: {field.secretName}, fieldValue: {field.fieldValue}. ";
                    continue;
                }
                else
                {
                    // Initilise a new field object to pass back the field name and decrypted value
                    AESSitsDecryptField resultField = new AESSitsDecryptField();

                    // Get secret from keyvault based on secretName 
                    string secretValue = await _kv.GetSecret(field.secretName);

                    // Check that we have a secret
                    if (String.IsNullOrEmpty(secretValue))
                    {
                        responseBatch.hasError = true;
                        responseBatch.error = $"Missing secret {field.secretName}. ";
                        continue;
                    }
                    // Add the field name to the new field object
                    resultField.fieldName = field.fieldName;
                    // Decrypt the fieldValue and extract the actual data (minus <stu_code>| prefix) then add to new field object
                    resultField.fieldValue = DoAESSitsExtract(DoAESDecrypt(secretValue, field.fieldValue));
 
                    responseSets.Add(resultField);

                }

            }

            if (responseSets.Count > 0)
            {
                // Add list of field objects to batch object
                responseBatch.fieldList = responseSets;
            }
            else 
            {
                responseBatch.hasError = true;
                responseBatch.error += "No fields decrypted";
            }

            // Return batch containing decrypted fields
            return new OkObjectResult(responseBatch);
        }


        private Aes CreateAESProvider(byte[] key)
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Key = key;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.ECB;
            return aes;
        }

        private string DoAESDecrypt(string strKey, string strEncryptedText)
        {
            byte[] key = Encoding.UTF8.GetBytes(strKey);
            byte[] data = Convert.FromBase64String(strEncryptedText);

            Aes aes = CreateAESProvider(key);
            ICryptoTransform decrypter = aes.CreateDecryptor();
            byte[] plain = decrypter.TransformFinalBlock(data, 0, data.Length);
            return Encoding.UTF8.GetString(plain);
        }

        private string DoAESSitsExtract(string combinedValue)
        {
            // Ensure that combinedValue has expected format for a decrypted SITS field
            // <8 digits> + '|' + (any character, except space)
            if (Regex.IsMatch(combinedValue, @"\d{8}\|[^\s]"))
            {
                // return everything after '|'
                return combinedValue.Split('|')[1];
            }
            else 
            {
                return null;
            }
        }
    }
}
