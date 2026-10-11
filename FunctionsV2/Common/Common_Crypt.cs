using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace FunctionsV2.Common
{
    public static class Common_Crypt
    {
        private class cryptRequest
        {
            public string passPhrase { get; set; }
            public string cipherText { get; set; }
        }


        private class cryptResponse
        {
            public string plainText { get; set; }
            public bool isEmpty { get; set; }
        }

        [FunctionName("Common_Crypt_Decrypt")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(cryptRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            cryptRequest request = Newtonsoft.Json.JsonConvert.DeserializeObject<cryptRequest>(requestBody);

            if (String.IsNullOrWhiteSpace(request.cipherText))
            {
                return new OkObjectResult(new cryptResponse() { plainText = null, isEmpty = true });
            }

            string _plainText = DecryptCombined(request.passPhrase, request.cipherText);

            if (String.IsNullOrWhiteSpace(_plainText))
            {
                return new OkObjectResult(new cryptResponse() { plainText = null, isEmpty = true });
            }
            else
            {
                return new OkObjectResult(new cryptResponse() { plainText = _plainText, isEmpty = false });
            }
        }





        public static string DecryptCombined(string passPhrase, string cipherText, string defaultString)
        {
            var x = DecryptCombined(passPhrase, cipherText);
            if (String.IsNullOrWhiteSpace(x))
                return defaultString;
            return x;
        }

        public static string DecryptCombined(string passPhrase, string cipherText)
        {

            if (cipherText is null)
                return null;


            byte[] passwordBytes = Encoding.Unicode.GetBytes(passPhrase);
            int version = BitConverter.ToInt32(StringToByteArray(cipherText.Substring(0, 8)), 0);
            byte[] encrypted = null;

            HashAlgorithm hashAlgo = null;
            SymmetricAlgorithm cryptoAlgo = null;
            int keySize = (version == 1 ? 16 : 32);

            if (version == 1)
            {
                hashAlgo = SHA1.Create();
                cryptoAlgo = TripleDES.Create();
                cryptoAlgo.IV = StringToByteArray(cipherText.Substring(8, 16));
                encrypted = StringToByteArray(cipherText.Substring(24));
            }
            else if (version == 2)
            {
                hashAlgo = SHA256.Create();
                cryptoAlgo = Aes.Create();
                cryptoAlgo.IV = StringToByteArray(cipherText.Substring(8, 32));
                encrypted = StringToByteArray(cipherText.Substring(40));
            }
            else
            {
                throw new Exception("Unsupported encryption");
            }

            cryptoAlgo.Padding = PaddingMode.PKCS7;
            cryptoAlgo.Mode = CipherMode.CBC;

            hashAlgo.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
            cryptoAlgo.Key = hashAlgo.Hash.Take(keySize).ToArray();

            byte[] decrypted = cryptoAlgo.CreateDecryptor().TransformFinalBlock(encrypted, 0, encrypted.Length);
            int decryptLength = BitConverter.ToInt16(decrypted, 6);
            UInt32 magic = BitConverter.ToUInt32(decrypted, 0);
            if (magic != 0xbaadf00d)
            {
                throw new Exception("Decrypt failed");
            }

            byte[] decryptedData = decrypted.Skip(8).ToArray();
            bool isUtf16 = (Array.IndexOf(decryptedData, (byte)0) != -1);
            string decryptText = (isUtf16 ? Encoding.Unicode.GetString(decryptedData) : Encoding.UTF8.GetString(decryptedData));

            return decryptText;
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
