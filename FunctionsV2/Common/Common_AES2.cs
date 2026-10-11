using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FunctionsV2.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace FunctionsV2.Common {
	public class Common_AES2 {
		private readonly ISecrets _secrets;

		public Common_AES2(ISecrets secrets) {
			_secrets = secrets;
		}

		public class AES2RequestBody {
			[JsonProperty("encryptedText")]
			public string EncryptedText { get; set; }
			[JsonProperty("keyName")]
			public string KeyName { get; set; }
			[JsonProperty("ivName")]
			public string IVName { get; set; }
		}

		/// <summary>
		/// Method <c>CreateAES</c> creates a custom AES object with the given key and IV, and
		/// also specifies the cipher mode and padding mode as confirmed with the supplier.
		/// </summary>
		/// <param name="key">The key to use for the AES object.</param>
		/// <param name="iv">The initialization vector to use for the AES object.</param>
		private static Aes CreateAES(byte[] key, byte[] iv) {
			Aes aes = Aes.Create();
			aes.KeySize = key.Length * 8; // Key and block sizes must be set before the key and IV values are assigned
			aes.BlockSize = 128;
			aes.Key = key; // The Key and IV values are set from the parameters
			aes.IV = iv;
			aes.Mode = CipherMode.CBC; // Cipher mode is set to CBC (Cipher Block Chaining) mode
			aes.Padding = PaddingMode.PKCS7; // Padding mode is set to PKCS7 (Public Key Cryptography Standard #7) padding
			return aes;
		}
		private static byte[] GenerateIV() {
			using Aes aes = Aes.Create();
			aes.GenerateIV();
			return aes.IV;
		}
		// For use if the IV is not known
		private static Aes CreateAES(byte[] key) {
			return CreateAES(key, GenerateIV());
		}

		[FunctionName("Common_AES2_Decrypt")]
		[OpenApiOperation]
		[OpenApiRequestBody(
			"application/json",
			typeof(AES2RequestBody),
			Required = true,
			Description = "The JSON object containing the text to decrypt, the name of the 'key' secret and the name of the 'iv' secret.",
			Example = typeof(AES2RequestBody)
		)]
		[OpenApiResponseWithBody(
			HttpStatusCode.OK,
			"text/json",
			typeof(string),
			Description = "The OK response, with the decrypted data"
		)]
		[OpenApiResponseWithBody(
			HttpStatusCode.BadRequest,
			"text/json",
			typeof(string),
			Description = "The Bad Request response, with the error message (if the request body is null, can't be parsed or decrypted"
		)]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/aes/v2/decrypt")]
			HttpRequest req,

			ILogger logger
		) {
			if (req.Body is null)
				return new BadRequestObjectResult("Request body cannot be null.");

			string json = await new StreamReader(req.Body).ReadToEndAsync();
			AES2RequestBody body;
			try {
				body = JsonConvert.DeserializeObject<AES2RequestBody>(json);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to deserialize JSON: {json}", json);
				return new BadRequestObjectResult($"An error occurred while deserializing JSON: {ex.Message}");
			}

			try {
				string data = await DecryptWithSecretAsync(body);
				return new OkObjectResult(data);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to decrypt {json}", json);
				return new BadRequestObjectResult($"An error occurred while decrypting {json}: {ex.Message}");
			}
		}

		private async Task<byte[]> GetSecretAsync(string secretName) {
			if (string.IsNullOrWhiteSpace(secretName))
				throw new ArgumentException("Secret name cannot be null or whitespace.");

			string secret = await _secrets.GetSecretAsync(secretName);
			if (string.IsNullOrWhiteSpace(secret))
				throw new ArgumentException($"Could not retrieve secret '{secretName}' from KeyVault.");

			byte[] secretBytes;
			try {
				secretBytes = Convert.FromBase64String(secret);
			} catch (FormatException ex) {
				throw new FormatException($"Failed to convert secret {secretName} into a byte array", ex);
			}
			return secretBytes;
		}
		public async Task<string> DecryptWithSecretAsync(AES2RequestBody body) {
			return await DecryptWithSecretAsync(body.EncryptedText, body.KeyName, body.IVName);
		}
		public async Task<string> DecryptWithSecretAsync(string encryptedText, string keyName, string ivName, string defaultValue) {
			if (encryptedText.IsNullOrWhiteSpace())
				return defaultValue;
			else
				return await DecryptWithSecretAsync(encryptedText, keyName, ivName);
		}
		public async Task<string> DecryptWithSecretAsync(string encryptedText, string keyName, string ivName) {
			if (encryptedText.IsNullOrWhiteSpace())
				return string.Empty;
			byte[] keyBytes = await GetSecretAsync(keyName);
			byte[] ivBytes = await GetSecretAsync(ivName);
			Aes aes = CreateAES(keyBytes, ivBytes);
			string output = await DecryptAsync(encryptedText, aes);
			return output;
		}

		/// <summary>
		/// Method <c>Decrypt</c> decrypts the given encrypted text using the provided AES object.
		/// </summary>
		/// <param name="encryptedText">The encrypted text to decrypt, as a Base64 string.</param>
		/// <param name="aes">The AES object to use for decryption.</param>
		/// <returns>The decrypted string, in plain-text</returns>
		public static async Task<string> DecryptAsync(string encryptedText, Aes aes) {
			if (string.IsNullOrWhiteSpace(encryptedText))
				throw new ArgumentException("Encrypted text cannot be null or whitespace.");

			string safeEncryptedText = encryptedText.Replace('-', '+').Replace('_', '/'); // Replaces the URL-safe characters with their original values
			switch (safeEncryptedText.Length % 4) { // Switch statement to determine the number of padding characters
				case 2: // If there are 2 padding characters, add 2 more
					safeEncryptedText += "==";
					break;
				case 3: // If there are 3 padding characters, add 1 more
					safeEncryptedText += "=";
					break;
			}
			using MemoryStream memoryStream = new(Convert.FromBase64String(safeEncryptedText)); // Creates a new memory stream using the given encrypted text, converted from a base64 string to a byte array
			using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read); // Creates a new CryptoStream using the given AES object and the memory stream for reading data
			using StreamReader streamReader = new(cryptoStream); // Creates a new StreamReader using the above CryptoStream
			string output = await streamReader.ReadToEndAsync(); // Reads the entire stream and returns the contents as a string
			try {
				await cryptoStream.FlushFinalBlockAsync(); // Flushes the final block of data from the CryptoStream
			} catch (Exception) { } // Ignore any exceptions
			return output; // Returns the decrypted output as a string
		}
	}
}
