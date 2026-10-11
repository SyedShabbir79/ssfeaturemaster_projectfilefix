using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FunctionsV2.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace FunctionsV2.Common {
	public class Common_AES3 {
		private readonly ISecrets _secrets;

		public Common_AES3(ISecrets secrets) {
			_secrets = secrets;
		}

        // A batch, containing a Dictionary or JSON object of one or more field/value pairs
        // e.g. "{data": {"<<field name>>":"<<field value>>", "<<field name>>":"<<field value>>"}}"
        private class AES3Batch
		{
            [JsonProperty("data")]
            public IDictionary<string, string> AES3Data { get; set; }
		}

        // A batch, containing a List or JSON array of encrypted fields, along with the names of the secrets required to decrypt them
        // e.g. "{keyName":"<<key>>", "ivName":"<<IV>>", data": {"<<field name>>":"<<field value>>", "<<field name>>":"<<field value>>"}}"
        private class AES3BatchRequest : AES3Batch
        {
            [JsonProperty("keyName")]
            public string AES3BatchKeyName { get; set; }
            [JsonProperty("ivName")]
            public string AES3BatchIVName { get; set; }
        }
        

        [FunctionName("Common_AES3_BatchDecrypt")]
        [OpenApiOperation]
        [OpenApiRequestBody(
			"application/json",
			typeof(AES3BatchRequest),
            Required = true,
			Description = "The JSON containg a number of fields to to decrypt, as well as the names of the secrets containing the required key and IV values.",
			Example = typeof(AES3BatchRequest)
		)]
		[OpenApiResponseWithBody(
			HttpStatusCode.OK,
			"application/json",
			typeof(AES3Batch),
			Description = "The OK response, with the decrypted fields"
		)]
		[OpenApiResponseWithBody(
			HttpStatusCode.BadRequest,
			"text/json",
			typeof(string),
			Description = "The Bad Request response, with the error message (if the request body is null, can't be parsed or decrypted"
		)]
        public async Task<IActionResult> BatchDecrypt(
			[HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/aes/v3/batchdecrypt")]
            HttpRequest req,
            ILogger logger
        )
        {
            // Check for a request body
            if (req.Body.Length == 0)
            {
                return new BadRequestObjectResult("Request body cannot be null.");
            }
            // Get JSON string from request body
            string json = await new StreamReader(req.Body).ReadToEndAsync();

            // Try to deserialize request body as AES3BatchRequest object (defined above)
            AES3BatchRequest batch;
            try
            {
                batch = JsonConvert.DeserializeObject<AES3BatchRequest>(json);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize JSON: {json}", json);
                return new BadRequestObjectResult($"An error occurred while deserializing JSON: {ex.Message}");
            }

            // Check for a missing key secret argument
            if (String.IsNullOrEmpty(batch.AES3BatchKeyName))
            {
                return new BadRequestObjectResult("Request body must contain a key name.");
            }
            // Check for a missing IV secret argument
            if (String.IsNullOrEmpty(batch.AES3BatchIVName))
            {
                return new BadRequestObjectResult("Request body must contain a IV name.");
            }

            // Get keys (field names) from data
            string[] dataKeys;
            if (null !=  batch.AES3Data) 
            {  
                dataKeys = batch.AES3Data.Keys.ToArray();

                // Check there is at least one field in the data
                if (dataKeys.Length == 0)
                {
                    return new BadRequestObjectResult("Request body must contain at least one field for decryption.");
                }
            }
            else 
            {
                return new BadRequestObjectResult("Request data object may not be null.");
            }
            
            // Initialise a batch object and a list of fields to return
			AES3Batch responseBatch = new AES3Batch();
            IDictionary<string, string> decryptedFields = new Dictionary<string, string>();

            // Get the key and IV secrets from KeyVault and convert to byte arrays
            Encoding encoding = Encoding.UTF8;
            byte[] key = encoding.GetBytes(await AES3GetSecretAsync(batch.AES3BatchKeyName));
            byte[] iv = encoding.GetBytes(await AES3GetSecretAsync(batch.AES3BatchIVName));
            
            // Create an AES object with the key and IV
            Aes aes = AES3CreateAES(key, iv);

            // Loop through fields in request batch 
            foreach (string fieldName in dataKeys)
			{
				if (String.IsNullOrEmpty(batch.AES3Data[fieldName]))
				{
					// Pass null field values straight back
                    decryptedFields.Add(fieldName, null);
				}
				else
				{
                    try
                    {
                        // Not convinced this is required, but leaving the code just in case - Duncan, 7/3/2024
                        // Cleanup URL safe charcaacters and padding
                        //string safeEcryptedString = AESSafeString(batch.AES3Data[fieldName]);
                        
                        // Decrypt current field value, update the field object and add it to the list to return
                        string plainValue = await AES3Decrypt(batch.AES3Data[fieldName], aes);
                        decryptedFields.Add(fieldName, plainValue);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to decrypt field {fieldName}: {batch.AES3Data[fieldName]}");
                        return new BadRequestObjectResult($"An error occurred while decrypting {fieldName}: {ex.Message}");
                    }
                }
			}

            // If there are decrypted fields to return, add them to the batch
			if (decryptedFields.Count > 0)
			{
				responseBatch.AES3Data = decryptedFields;
			}
			else
			{
                return new BadRequestObjectResult($"No fields successfully decrypted {json}");
            }

            // Return our batch of decrypted fields
            return new OkObjectResult(responseBatch);
        }

        /// <summary>
        /// Method <c>AES3GetSecretAsync</c> Gets the value for the given Secret from KeyVault.
        /// </summary>
        /// <param name="secretName">The name of the required secret.</param>
        /// <returns>The string value of the the Secret.</returns>
        private async Task<string> AES3GetSecretAsync(string secretName)
        {
            if (string.IsNullOrWhiteSpace(secretName))
                throw new ArgumentException("Secret name cannot be null or whitespace.");

            string secret = await _secrets.GetSecretAsync(secretName);
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException($"Could not retrieve secret '{secretName}' from KeyVault.");

            return secret;
        }

        /// <summary>
        /// Method <c>AES3Decrypt</c> decrypts the given encrypted text using the provided AES object.
        /// </summary>
        /// <param name="encryptedText">The encrypted text to decrypt, as a Base64 string.</param>
        /// <param name="aes">The AES object to use for decryption.</param>
        /// <returns>The decrypted string, in plain-text</returns>
        public static async Task<string> AES3Decrypt(string encryptedText, Aes aes)
		{
            // Convert the encrypted string to bytes
            byte[] encryptedTextBytes = Convert.FromBase64String(encryptedText);

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream msDecrypt = new MemoryStream(encryptedTextBytes);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new StreamReader(csDecrypt);
            
            // Return decrypted field as string
            return await srDecrypt.ReadToEndAsync();
        }

        /// <summary>
        /// Method <c>AES3CreateAES</c> creates a custom AES object with the given key and IV, and
        /// also specifies the cipher mode and padding mode.
        /// </summary>
        /// <param name="key">The key to use for the AES object.</param>
        /// <param name="iv">The initialization vector to use for the AES object.</param>
        private static Aes AES3CreateAES(byte[] key, byte[] iv)
        {
            Aes aes = Aes.Create();
            aes.KeySize = key.Length * 8; // Key and block sizes must be set before the key and IV values are assigned
            aes.BlockSize = 128;
            aes.Key = key; // The Key and IV values are set from the parameters
            aes.IV = iv;
            aes.Mode = CipherMode.CBC; // Cipher mode is set to CBC (Cipher Block Chaining) mode
            aes.Padding = PaddingMode.PKCS7; // Padding mode is set to PKCS7 (Public Key Cryptography Standard #7) padding
            return aes;
        }

        // Not convinced this is required, but leaving the code just in case - Duncan, 7/3/2024
        /// <summary>
        /// Method <c>AES3SafeString</c> Replaces URL safe character substitutions
        /// and tops up padding where string length is not divisible by four.
        /// </summary>
        /// <param name="encryptedString">The encypted string for processing.</param>
        //private static string AESSafeString(string encryptedString)
        //{
        //    string safeEncryptedString = encryptedString.Replace('-', '+').Replace('_', '/'); // Replaces the URL-safe characters with their original values
        //    switch (safeEncryptedString.Length % 4)
        //    { // Switch statement to determine the number of padding characters
        //        case 2: // If there are 2 padding characters, add 2 more
        //            safeEncryptedString += "==";
        //            break;
        //        case 3: // If there are 3 padding characters, add 1 more
        //            safeEncryptedString += "=";
        //            break;
        //    }
        //    return safeEncryptedString;
        //}

    }
}
