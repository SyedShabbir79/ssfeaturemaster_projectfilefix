using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace FunctionsV2.Helpers {
	public interface ISecrets {
		public Uri VaultURI { get; set; }
		public SecretClient Client { get; set; }
		public string GetSecret(string key);
		public Task<string> GetSecretAsync(string key);
		public void SetSecret(string key, string value);
		public Task SetSecretAsync(string key, string value);
		public Azure.Pageable<SecretProperties> GetPropertiesOfSecrets();
		public Azure.AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync();
		public DeletedSecret GetDeletedSecret(string key);
		public Task<DeletedSecret> GetDeletedSecretAsync(string key);
		public void UpdateSecretProperties(SecretProperties properties);
		public Task UpdateSecretPropertiesAsync(SecretProperties properties);

	}

	public class Secrets : ISecrets {
		public Uri VaultURI { get; set; }
		public SecretClient Client { get; set; }
		public Secrets(IConfiguration config) {
			VaultURI = new(config["kvEndpoint"]);
			Client = new SecretClient(VaultURI, new DefaultAzureCredential());
			Client.GetPropertiesOfSecretsAsync(); // Called to perform initial authentication
		}

		public string GetSecret(string key) {
			try {
				var secret = Client.GetSecret(key);
				if (secret is null)
					return string.Empty;
				if (secret.Value is null)
					return string.Empty;
				if (secret.Value.Value is null)
					return string.Empty;

				return secret.Value.Value;
			} catch (Azure.RequestFailedException ex) {
				if (ex.Message.Contains("NotFound")) return string.Empty;
				else throw ex;
			}
		}

		public async Task<string> GetSecretAsync(string key) {
			try {
				var secret = await Client.GetSecretAsync(key);
				if (secret is null || secret.Value is null || secret.Value.Value is null)
					return string.Empty;
				else 
					return secret.Value.Value;
			} catch (Azure.RequestFailedException ex) {
				if (ex.Message.Contains("NotFound")) return string.Empty;
				else throw ex;
			}
		}

		public void SetSecret(string key, string value) => Client.SetSecret(new KeyVaultSecret(key, value));
		public async Task SetSecretAsync(string key, string value) => await Client.SetSecretAsync(new KeyVaultSecret(key, value));

		public Azure.Pageable<SecretProperties> GetPropertiesOfSecrets() => Client.GetPropertiesOfSecrets();
		public Azure.AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync() => Client.GetPropertiesOfSecretsAsync();

		public DeletedSecret GetDeletedSecret(string key) => Client.GetDeletedSecret(key);
		public async Task<DeletedSecret> GetDeletedSecretAsync(string key) => await Client.GetDeletedSecretAsync(key);

		public void UpdateSecretProperties(SecretProperties properties) => Client.UpdateSecretProperties(properties);
		public async Task UpdateSecretPropertiesAsync(SecretProperties properties) => await Client.UpdateSecretPropertiesAsync(properties);
	}
}