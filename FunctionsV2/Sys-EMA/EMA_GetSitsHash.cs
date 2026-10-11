using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using static FunctionsV2.Sys_EMA.EMA_Classes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace FunctionsV2.Sys_EMA
{
    public static class EMA_GetSitsHash
    {
        [FunctionName("EMA_GetSitsHash")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Example: Access a field in the JSON
            string param = data?.param?.ToString();
            string functionname = data?.functionname?.ToString();


            string environment = Environment.GetEnvironmentVariable("environment");
            string SITSUser;
            string SITSpassword;
            string SITSenvironment;
            string keyphrase;

            switch (environment)
            {
                case "dev":
                    SITSUser = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-UserName"; //"SITSUSer"; 
                    SITSpassword = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Password"; //"SITSPassword";
                    SITSenvironment = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Environment"; //SITSenvironmentsecret";
                    keyphrase = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Keyphrase";// keyphrasesecret";
                    break;
                case "uat":
                    SITSUser = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-UserName"; //"SITSUSer"; 
                    SITSpassword = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Password"; //"SITSPassword";
                    SITSenvironment = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Environment"; //SITSenvironmentsecret";
                    keyphrase = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Keyphrase";// keyphrasesecret";
                    break;
                case "prod-uks":
                    SITSUser = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-UserName"; //"SITSUSer"; 
                    SITSpassword = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Password"; //"SITSPassword";
                    SITSenvironment = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Environment"; //SITSenvironmentsecret";
                    keyphrase = "UoR-Common-SITSWSF-BZ-EMA-MAV-A-Keyphrase";// keyphrasesecret";
                    break;
                default:
                    return new BadRequestObjectResult("Unknown environment.");
            }

            string keyVaultName = Environment.GetEnvironmentVariable("kvEndpoint");
            //var kvUri = $"https://{keyVaultName}.vault.azure.net/";
            var kvUri = $"{keyVaultName}/";

            //// Create a SecretClient using DefaultAzureCredential (which will use the managed identity)
            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

            //// Retrieve the secret from Key Vault
            KeyVaultSecret username = await client.GetSecretAsync(SITSUser);
            KeyVaultSecret password = await client.GetSecretAsync(SITSpassword);
            KeyVaultSecret SITSEnvironment = await client.GetSecretAsync(SITSenvironment);
            KeyVaultSecret KeyPhrase = await client.GetSecretAsync(keyphrase);

            //return new OkObjectResult($"The secret value for {environment} is: {secret.Value}");

            

            string Hashformat = "{0}{1}|{2}|" + functionname + "|{3}|" + param;

            
            string hashvariable = string.Format(Hashformat, username.Value, password.Value, SITSEnvironment.Value, KeyPhrase.Value);




            string hashinput;
            byte[] tmpSource;
            byte[] tmpHash;
            hashinput = hashvariable;
            //Create a byte array from source data
            tmpSource = ASCIIEncoding.ASCII.GetBytes(hashinput);

            //Compute hash based on source data
            using (SHA512 shaM = new SHA512Managed())
            {
                tmpHash = shaM.ComputeHash(tmpSource);
            }
            // Console.WriteLine(tmpHash);
            Console.WriteLine(ByteArrayToString(tmpHash));

            static string ByteArrayToString(byte[] arrInput)
            {
                int i;
                StringBuilder sOutput = new StringBuilder(arrInput.Length);
                for (i = 0; i < arrInput.Length; i++)
                {
                    sOutput.Append(arrInput[i].ToString("X2"));
                }
                return sOutput.ToString();
            }
            string responseMessage = ByteArrayToString(tmpHash);

            return new OkObjectResult(responseMessage);
        }
    }
}
