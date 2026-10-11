using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure;
//using ImpromptuInterface;
using Renci.SshNet.Common;
using System.Net.Sockets;
using Microsoft.Extensions.Azure;
using static FunctionsV2.Common.Common_SFTP;
using Renci.SshNet.Sftp;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;
using static System.Net.WebRequestMethods;
using System.Text.RegularExpressions;
using FunctionsV2.Helpers;

namespace FunctionsV2.Common {
	public class Common_SFTP3 {
		public SecretClient secretClient;

		public Common_SFTP3(IConfiguration config) {
			secretClient = new SecretClient(
				new Uri(config.GetConnectionStringOrSetting("kvEndpoint")),
				new DefaultAzureCredential(
					new DefaultAzureCredentialOptions {
						// Disable the token credential that we don't use
						ExcludeEnvironmentCredential = true,
						ExcludeSharedTokenCacheCredential = true,
						ExcludeVisualStudioCodeCredential = true,
						ExcludeAzurePowerShellCredential = true,
						ExcludeAzureCliCredential = true,

						ExcludeInteractiveBrowserCredential = false,
						ExcludeVisualStudioCredential = false,
						ExcludeManagedIdentityCredential = false
					}
				)
			);
		}

		public class SFTP3Credential {
			public string hostname { get; set; }
			public string username { get; set; }
			public string passwordSecretName { get; set; }
			public string keySecretName { get; set; }
			public int port { get; set; } = 22;
		}


		public class SFTP3WriteFileRequest : SFTP3Credential {
			public string filePath { get; set; }
			public string base64Content { get; set; }

			internal byte[] fileContent {
				get {
					return Convert.FromBase64String(base64Content);
				}
			}
		}

		public class SFTP3OverwriteFileRequest : SFTP3WriteFileRequest {
			public bool overwrite { get; set; } = true;
		}

		public class SFTP3ReadFileRequest : SFTP3Credential {
			public string filePath { get; set; }
		}
		public class SFTP3ListDirRequest : SFTP3Credential {
			public string filePath { get; set; }
			public bool excludeZeroFiles { get; set; } = false;
			public string fileNamePattern { get; set; } = ".*";
		}


        public class SFTP3File
        {
            public string fileName { get; set; }
            public string fullName { get; set; }
            public string extension { get; set; }
            public bool isDirectory { get; set; }
            public long size { get; set; }
            public string base64Content { get; set; }
            public DateTime lastModified { get; set; }

            public string checksum
            {
                get
                {
                    if (base64Content == null) return null;
                    StringBuilder output = new();
                    using MD5 md5 = MD5.Create();
                    byte[] bytes = Convert.FromBase64String(base64Content);
                    byte[] hash = md5.ComputeHash(bytes);
                    foreach (byte b in hash) output.Append(b.ToString("X2"));
                    return output.ToString();
                }
            }

            // Constructor that accepts ISftpFile
            public SFTP3File(ISftpFile input, SftpClient client)
            {
                fileName = input.Name;
                fullName = input.FullName;
                extension = Path.GetExtension(input.Name);
                isDirectory = input.IsDirectory;
                size = input.Attributes.Size;
                lastModified = input.LastWriteTimeUtc;

                if (!client.IsConnected)
                {
                    client.Connect();
                }

                base64Content = input.IsRegularFile ? Convert.ToBase64String(client.ReadAllBytes(fullName)) : null;
            }
        }

        public async Task<Renci.SshNet.ConnectionInfo> GetConnectionInfo(SFTP3Credential credential, ILogger log) {
			var authenticationMethods = new List<AuthenticationMethod>();
			if (credential.passwordSecretName != null)
				authenticationMethods.Add(new PasswordAuthenticationMethod(credential.username, await GetSecret(credential.passwordSecretName, log)));
			if (credential.keySecretName != null)
				authenticationMethods.Add(new PrivateKeyAuthenticationMethod(credential.username, await GetPrivateKeyFile(credential.keySecretName, log)));

			if (authenticationMethods.Count == 0) {
				var message = "Credential must contain either a password or a key!";
				log.LogCritical(message);
				throw new Exception(message);
			}

			return new Renci.SshNet.ConnectionInfo(credential.hostname, credential.port, credential.username, authenticationMethods.ToArray());
		}

		public async Task<PrivateKeyFile> GetPrivateKeyFile(string secretName, ILogger log) {
			try {
				var secret = await secretClient.GetSecretAsync(secretName);
				return new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(secret.Value.Value)));
			} catch (Exception ex) {
				log.LogError("Failed to get {secretName} from KeyVault: {exception}", secretName, ex.Message);
				return null;
			}
		}
		public async Task<string> GetSecret(string name, ILogger log) {
			KeyVaultSecret secret;
			try {
				log.LogInformation("Getting secret");
				secret = await secretClient.GetSecretAsync(name);
				log.LogInformation($"Got secret: {secret.Name}");
				return secret.Value;
			} catch (Exception ex) {
				log.LogCritical($"Failed to get secret {name}: {ex.Message}");
				return null;
			}
		}

		public IActionResult Connect(SftpClient client, ILogger log) {
			try {
				log.LogInformation($"Attempting to connect to {client.ConnectionInfo.Username}@{client.ConnectionInfo.Host}:{client.ConnectionInfo.Port}...");
				client.Connect();
				log.LogInformation($"Connected to {client.ConnectionInfo.Username}@{client.ConnectionInfo.Host}:{client.ConnectionInfo.Port}.");
				return null;
			} catch (SshConnectionException ex) {
				string message = $"SSHConnectionException ({ex.DisconnectReason}) {ex.Message}";
				log.LogError(message);
				return new UnprocessableEntityObjectResult(message);
			} catch (SshAuthenticationException ex) {
				string message = $"SshAuthenticationException {ex.Message}";
				log.LogError(message);
				return new UnprocessableEntityObjectResult(message);
			} catch (SocketException ex) {
				string message = $"SocketException ({ex.SocketErrorCode}) {ex.Message}";
				log.LogError(message);
				return new UnprocessableEntityObjectResult(message);
			} catch (Exception ex) {
				string message = $"Unknown Exception: {ex.Message}";
				log.LogError(message);
				return new BadRequestObjectResult(message);
			}
		}



		[FunctionName("Common_SFTP3_Read")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTP3ReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(SFTP3File))]
		public async Task<IActionResult> SFTP3_Read(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v3/read"
			)]
			HttpRequest
			req,

			ILogger log
		) {

			string body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTP3ReadFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTP3ReadFileRequest>(body);
				log.LogInformation($"Got data: {data.username} ({data.passwordSecretName} | {data.keySecretName})");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(await GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;

			return new OkObjectResult(
				new SFTP3File(
                   client.Get(data.filePath), client)
				);
		}

		[FunctionName("Common_SFTP3_Write")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTP3WriteFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
		public async Task<IActionResult> SFTP3_Write(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v3/write"
			)]
			HttpRequest
			req,

			ILogger log
		) {
			string body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTP3WriteFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTP3WriteFileRequest>(body);
				log.LogInformation($"Got data: {data.username} ({data.passwordSecretName} | {data.keySecretName})");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(await GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;


			try {
				client.WriteAllBytes(data.filePath, data.fileContent);
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex);
			}
			return new OkObjectResult(true);

		}

		[FunctionName("Common_SFTP3_Overwrite")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTP3OverwriteFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
		public async Task<IActionResult> SFTP3_Overwrite(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v3/overwrite"
			)]
			HttpRequest
			req,

			ILogger log
		) {
			string body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTP3OverwriteFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTP3OverwriteFileRequest>(body);
				log.LogInformation($"Got data: {data.username} ({data.passwordSecretName} | {data.keySecretName}), overwrite: {data.overwrite}");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(await GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;
			try {
				if (client.Exists(data.filePath)) {
					if (data.overwrite) {
						log.LogInformation($"Deleted existing file: {data.filePath}");
						client.Delete(data.filePath);
					} else {
						log.LogInformation($"File already exists: {data.filePath}");
						return new BadRequestObjectResult($"File already exists: {data.filePath}");
					}
				}
				log.LogInformation($"Writing file: {data.filePath} to server: {data.hostname}");
				client.WriteAllBytes(data.filePath, data.fileContent);
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex);
			}
			return new OkObjectResult(true);
		}

		[FunctionName("Common_SFTP3_Exists")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTP3ReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
		public async Task<IActionResult> SFTP3_Exists(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v3/exists"
			)]
			HttpRequest
			req,

			ILogger log
		) {
			string body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTP3ReadFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTP3ReadFileRequest>(body);
				log.LogInformation($"Got data: {data.username} ({data.passwordSecretName} | {data.keySecretName})");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(await GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;

			bool exists = false;
			try {
				exists = client.Exists(data.filePath);
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex);
			}
			return new OkObjectResult(exists);
		}

		[FunctionName("Common_SFTP3_List")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTP3ListDirRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(SFTP3File))]
		public async Task<IActionResult> SFTP3_List(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v3/list"
			)]
			HttpRequest
			req,

			ILogger log
		) {
			var body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTP3ListDirRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTP3ListDirRequest>(body);
				log.LogInformation($"Got data: {data.username} ({data.passwordSecretName} | {data.keySecretName})");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(await GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;

			List<SFTP3File> output = new();
			IEnumerable<ISftpFile> files;
			try {
				files = client.ListDirectory(data.filePath);
			} catch (Exception ex) {
				log.LogError(ex.Message, ex);
				return new BadRequestObjectResult(ex);
			}

			foreach (var file in files) {
				// Ignore anything that isn't a file
				if (!file.IsRegularFile) continue;
				// Ignore anything that doesn't match the provided pattern (default: .*)
				if (!Regex.Match(file.Name, data.fileNamePattern).Success) continue;
				try {
					SFTP3File sftpFile = new(file, client);
					// Ignore any files that are empty (if requested)
					if (data.excludeZeroFiles && sftpFile.size == 0) continue;
					output.Add(sftpFile);
        } catch (Exception ex) {
					log.LogWarning(ex.Message, ex);
				}
			}

			return new OkObjectResult(output);

		}

		[FunctionName("Common_SFTP3_Delete")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTP3ReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
		public async Task<IActionResult> SFTP3_Delete(
				[HttpTrigger(
					AuthorizationLevel.Function,
					"POST",
					Route = "common/sftp/v3/delete"
				)]
				HttpRequest
				req,

				ILogger log
		) {
			var body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTP3ReadFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTP3ReadFileRequest>(body);
				log.LogInformation($"Got data: {data.username} ({data.passwordSecretName} | {data.keySecretName})");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(await GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;

			try {
				client.Delete(data.filePath);
			} catch (SftpPathNotFoundException ex) {
				log.LogCritical($"Requested file path not found: {data.filePath}");
				return new BadRequestObjectResult($"Requested file path not found: {data.filePath}");
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex);
			}
			return new OkObjectResult(true);
		}
	}
}