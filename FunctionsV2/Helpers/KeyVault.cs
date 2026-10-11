using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Azure;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;

namespace FunctionsV2.Helpers
{
    //Interface
    public interface IKeyVault
    {
        Task<string> GetSecret(string key);
        //Task<KeyVaultCertificateWithPolicy> GetCertificate(string key);

        X509Certificate2 GetCertificate(string key);
    }

    //Service
    public class KeyVault : IKeyVault
    {
        private readonly IConfiguration _config;
        private readonly SecretClient _secretClient;

        public KeyVault(IConfiguration config)
        {
            _config = config;

            // Initialize the SecretClient using DefaultAzureCredential
            _secretClient = new SecretClient(
                new Uri(_config["kvEndpoint"]),
                new DefaultAzureCredential()
            );
        }

        public async Task<string> GetSecret(string key)
        {
            try
            {
                // Retrieve the secret from Key Vault
                KeyVaultSecret secret = await _secretClient.GetSecretAsync(key);
                return secret.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Return an empty string if the secret is not found
                return string.Empty;
            }
            catch (Exception ex)
            {
                // Log and rethrow other exceptions
                throw new InvalidOperationException("An error occurred while retrieving the secret.", ex);
            }
        }

        public X509Certificate2 GetCertificate(string key)
        {
            KeyVaultSecret secret = _secretClient.GetSecretAsync(key).Result;
            if (secret.Value == null)
                return null;

            byte[] certificate = Convert.FromBase64String(secret.Value);
            X509Certificate2 x509 = new X509Certificate2(certificate, (string)null, X509KeyStorageFlags.MachineKeySet);
            return x509;
        }
    }
}
