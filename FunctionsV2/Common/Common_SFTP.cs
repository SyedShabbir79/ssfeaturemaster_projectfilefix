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
using RestSharp;

namespace FunctionsV2.Common {
	public class Common_SFTP {
		private readonly IConfiguration config;
		public static SecretClient secretClient;
		public Common_SFTP(IConfiguration config) {
			this.config = config;
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

			//secretClient.GetDeletedSecretsAsync();
		}

		public class SFTPCredentials {
			public string hostname { get; set; }
			public string username { get; set; }
			public string secretName { get; set; }
			// public string secretValue { get; set; }
			public bool isSecretIDFile { get; set; } = false;
			public int port { get; set; } = 22;
		}


		public class SFTPWriteFileRequest : SFTPCredentials {
			public string filePath { get; set; }
			public string base64Content { get; set; }

			internal byte[] fileContent {
				get {
					return Convert.FromBase64String(base64Content);
				}
			}
		}

		public class SFTPOverwriteFileRequest : SFTPWriteFileRequest {
			public bool overwrite { get; set; } = true;
		}

        public class SFTPReadFileRequest : SFTPCredentials {
			public string filePath { get; set; }
		}
		public class SFTPListDirRequest : SFTPCredentials {
			public string filePath { get; set; }
			public bool excludeZeroFiles { get; set; } = false;
			public string fileNamePattern { get; set; } = ".*";
		}


        public class SFTPFile
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
           
            public SFTPFile(SftpFile input, SftpClient client)
            {
                Initialize(input, client);
            }

            public SFTPFile(ISftpFile input, SftpClient client)
            {
                if (input is SftpFile sftpFile)
                {
                    Initialize(sftpFile, client);
                }
                else
                {
                    throw new ArgumentException("The input must be of type SftpFile", nameof(input));
                }
            }

            private void Initialize(SftpFile input, SftpClient client)
            {
                fileName = input.Name;
                fullName = input.FullName;
                extension = Path.GetExtension(input.Name);
                isDirectory = input.IsDirectory;
                size = input.Length;
                lastModified = input.LastWriteTimeUtc;

                if (!client.IsConnected)
                {
                    client.Connect();
                }

                base64Content = input.IsRegularFile ? Convert.ToBase64String(client.ReadAllBytes(fullName)) : null;
            }
        }


        public class ColinSftpFile {
			public string Filename { get; set; }
			public string Fullname { get; set; }
			public string Extension { get; set; }
			public bool IsDirectory { get; set; }
			public long Size { get; set; }
			public string base64Content { get; set; }
			public string checksum { get; set; }

			public DateTime lastModified { get; set; }
		}
		public class ColinSftpListDirRequest : SFTPCredentials {
			public string folder { get; set; }
			public bool? excludeFolders { get; set; }
			public bool? excludeZeroFiles { get; set; }
			public string fileNamePattern { get; set; }
		}

		public static Renci.SshNet.ConnectionInfo GetConnectionInfo(SFTPCredentials credentials, ILogger log) {
			string secret = GetSecret(credentials, log);
			AuthenticationMethod authenticationMethod;
			if (!credentials.isSecretIDFile) authenticationMethod = new PasswordAuthenticationMethod(credentials.username, secret);
			else authenticationMethod = new PrivateKeyAuthenticationMethod(
				credentials.username,
				new PrivateKeyFile(
					new MemoryStream(
						Encoding.ASCII.GetBytes(secret)
					)
				)
			);
			Renci.SshNet.ConnectionInfo connectionInfo = new(credentials.hostname, credentials.port, credentials.username, authenticationMethod);
			return connectionInfo;
		}

		public static IActionResult Connect(SftpClient client, ILogger log) {
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

		public static string GetSecret(SFTPCredentials input, ILogger log) {
			KeyVaultSecret secret;
			try {
				log.LogInformation("Getting secret");
				secret = secretClient.GetSecretAsync(input.secretName).Result;
				log.LogInformation($"Got secret: {secret}");
				return secret.Value;
			} catch (Exception ex) {
				log.LogCritical($"Failed to get secret {input.secretName}: {ex.Message}");
				return null;
			}
		}

		[FunctionName("Common_SFTP2_Read")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(SFTPFile))]
		public async Task<IActionResult> SFTP2_Read(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v2/read"
			)]
			HttpRequest
			req,

			ILogger log
		) {

			string body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTPReadFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTPReadFileRequest>(body);
				log.LogInformation($"Got data: {data.username}, {data.secretName}");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;

			return new OkObjectResult(
				new SFTPFile(
                             client.Get(data.filePath), client)
				);
		}

		[FunctionName("Common_SFTP2_Write")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPWriteFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
		public async Task<IActionResult> SFTP2_Write(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v2/write"
			)]
			HttpRequest
			req,

			ILogger log
		) {
			string body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTPWriteFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTPWriteFileRequest>(body);
				log.LogInformation($"Got data: {data.username}, {data.secretName}");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;


			try {
				client.WriteAllBytes(data.filePath, data.fileContent);
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex);
			}
			return new OkObjectResult(true);

		}

        [FunctionName("Common_SFTP2_Overwrite")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(SFTPOverwriteFileRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
        public async Task<IActionResult> SFTP2_OverWrite(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "POST",
                Route = "common/sftp/v2/overwrite"
            )]
            HttpRequest
            req,

            ILogger log
        )
        {
            string body = await req.ReadAsStringAsync();
            log.LogInformation($"Got body: {body}");
            SFTPOverwriteFileRequest data = null;
            try
            {
                data = System.Text.Json.JsonSerializer.Deserialize<SFTPOverwriteFileRequest>(body);
                log.LogInformation($"Got data: {data.username}, {data.secretName}, (overwrite) {data.overwrite}");
            }
            catch (Exception ex)
            {
                log.LogInformation($"Exception: {ex.Message}");
            }

            using SftpClient client = new(GetConnectionInfo(data, log));
            IActionResult connectOutput = Connect(client, log);
            if (connectOutput != null) return connectOutput;

            try
            {
				if (client.Exists(data.filePath)) 
				{
					if (data.overwrite)
					{
                        log.LogInformation($"Deleted existing file: {data.filePath}");
                        client.Delete(data.filePath);
                    }
					else
					{
                        log.LogInformation($"File already exists: {data.filePath}");
                        return new BadRequestObjectResult($"File already exists: {data.filePath}");
					}
				}
				log.LogInformation($"Writing file: {data.filePath} to server: {data.hostname}");
                client.WriteAllBytes(data.filePath, data.fileContent);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
            return new OkObjectResult(true);

        }

        [FunctionName("Common_SFTP2_Exists")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
		public async Task<IActionResult> SFTP2_Exists(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v2/exists"
			)]
			HttpRequest
			req,

			ILogger log
		) {
			string body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTPReadFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTPReadFileRequest>(body);
				log.LogInformation($"Got data: {data.username}, {data.secretName}");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(GetConnectionInfo(data, log));
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

		[FunctionName("Common_SFTP2_List")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPListDirRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(SFTPFile))]
		public async Task<IActionResult> SFTP2_List(
			[HttpTrigger(
				AuthorizationLevel.Function,
				"POST",
				Route = "common/sftp/v2/list"
			)]
			HttpRequest
			req,

			ILogger log
		) {
			var body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTPListDirRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTPListDirRequest>(body);
				log.LogInformation($"Got data: {data.username}, {data.secretName}");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(GetConnectionInfo(data, log));
			IActionResult connectOutput = Connect(client, log);
			if (connectOutput != null) return connectOutput;

			List<SFTPFile> output = new();
			IEnumerable<ISftpFile> files;
			try {
				files = client.ListDirectory(data.filePath);
			} catch (Exception ex) {
				log.LogError(ex.Message, ex);
				return new BadRequestObjectResult(ex);
			}

			foreach (var file in files)
			{
				// Ignore anything that isn't a file
				if (!file.IsRegularFile) continue;
				// Ignore anything that doesn't match the provided pattern (default: .*)
				if (!Regex.Match(file.Name, data.fileNamePattern).Success) continue;
				try
				{
					SFTPFile sftpFile = new (file, client);
					// Ignore any files that are empty (if requested)
					if (data.excludeZeroFiles && sftpFile.size == 0) continue;
					output.Add(sftpFile);
				}
				catch (Exception ex)
				{
					log.LogWarning(ex.Message, ex);
				}
			}

			return new OkObjectResult(output);

		}

		[FunctionName("Common_SFTP2_Delete")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK | HttpStatusCode.BadRequest, "application/json", typeof(bool))]
		public async Task<IActionResult> SFTP2_Delete(
				[HttpTrigger(
					AuthorizationLevel.Function,
					"POST",
					Route = "common/sftp/v2/delete"
				)]
				HttpRequest
				req,

				ILogger log
		) {
			var body = await req.ReadAsStringAsync();
			log.LogInformation($"Got body: {body}");
			SFTPReadFileRequest data = null;
			try {
				data = System.Text.Json.JsonSerializer.Deserialize<SFTPReadFileRequest>(body);
				log.LogInformation($"Got data: {data.username}, {data.secretName}");
			} catch (Exception ex) {
				log.LogInformation($"Exception: {ex.Message}");
			}

			using SftpClient client = new(GetConnectionInfo(data, log));
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

		[FunctionName("Common_SFTP_ListDirectory")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(ColinSftpListDirRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<ColinSftpFile>))]
		public async Task<IActionResult> listDirectory([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/sftp/listDirectory")] HttpRequestMessage req, ILogger log) {
			try {

				string opdgProxyHostname = config["opdgProxyHostname"];
				log.LogInformation($"opdgProxy: {opdgProxyHostname}");

				ColinSftpListDirRequest listDIR = await req.Content.ReadAsAsync<ColinSftpListDirRequest>();

				var rc = new RestSharp.RestClient($"http://{config["opdgProxyHostname"]}:62001");
				var r = new RestSharp.RestRequest(@$"/api/sftp/listDirectory");
				r.AddJsonBody(listDIR);
				var x = await rc.ExecutePostAsync<List<ColinSftpFile>>(r);



				if (x.ErrorException != null) {
					return new BadRequestObjectResult(x);
					//throw new Exception(x.Content, x.ErrorException);
				}



				if (x.StatusCode == HttpStatusCode.OK)
					return new OkObjectResult(x.Data);

				return new BadRequestObjectResult(x);
				//throw new Exception(x.Content);




				//

				//string password = await _kv.GetSecret(request.secretName);

				//var connectionInfo = new Renci.SshNet.ConnectionInfo(request.hostname, request.username, new PasswordAuthenticationMethod(request.username, password));
				//using (var client = new SftpClient(connectionInfo))
				//{
				//    client.Connect();

				//    var dir = client.ListDirectory(request.folder);
				//    dir = dir.Where(f => !request.excludeFolders.HasValue || request.excludeFolders.Value && f.IsDirectory == false);
				//    dir = dir.Where(f => !request.excludeZeroFiles.HasValue || f.Length > 0);
				//    dir = dir.Where(f => String.IsNullOrWhiteSpace(request.filenamePattern) || new System.Text.RegularExpressions.Regex(request.filenamePattern).IsMatch(f.Name));

				//    return new OkObjectResult(dir.Select(f => new sftpFile()
				//    {
				//        Filename = f.Name,
				//        IsDirectory = f.IsDirectory,
				//        Fullname = f.FullName,
				//        Size = f.Length,
				//        Extension = Path.GetExtension(f.FullName),
				//        lastModified = f.LastWriteTimeUtc
				//    }).ToList());
				//}
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex.Message);
				//throw;
			}
		}

		[FunctionName("Common_SFTP_ReadFile")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ColinSftpFile))]
		public async Task<IActionResult> readFile([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/sftp/readFile")] HttpRequestMessage req, ILogger log) {
			try {
				SFTPReadFileRequest readFile = await req.Content.ReadAsAsync<SFTPReadFileRequest>();

				var rc = new RestSharp.RestClient($"http://{config["opdgProxyHostname"]}:62001");
				var r = new RestSharp.RestRequest(@$"/api/sftp/readFile");
				r.AddJsonBody(readFile);
				var x = await rc.ExecutePostAsync<ColinSftpFile>(r);

				if (x.ErrorException != null)
					throw x.ErrorException;


				if (x.StatusCode == HttpStatusCode.OK) {
					return new OkObjectResult(x.Data);
				}

				throw new Exception(x.Content);

				//sftpReadFileRequest request = await System.Text.Json.JsonSerializer.DeserializeAsync<sftpReadFileRequest>(req.Body);

				//string password = await _kv.GetSecret(request.secretName);

				//var connectionInfo = new Renci.SshNet.ConnectionInfo(request.hostname, request.username, new PasswordAuthenticationMethod(request.username, password));
				//using (var client = new SftpClient(connectionInfo))
				//{
				//    client.Connect();

				//    var f = client.Get(request.filePath);
				//    byte[] c = client.ReadAllBytes(request.filePath);

				//    using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
				//    {
				//        byte[] hashBytes = md5.ComputeHash(c);

				//        StringBuilder sb = new StringBuilder();
				//        for (int i = 0; i < hashBytes.Length; i++)
				//        {
				//            sb.Append(hashBytes[i].ToString("x2"));
				//        }

				//        return new OkObjectResult(new sftpFile()
				//        {
				//            Filename = f.Name,
				//            IsDirectory = f.IsDirectory,
				//            Fullname = f.FullName,
				//            Size = f.Length,
				//            Extension = Path.GetExtension(f.FullName),
				//            base64Content = Convert.ToBase64String(c),
				//            checksum = sb.ToString(),
				//            lastModified = f.LastWriteTimeUtc

				//        });
				//    }
				//}
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex.Message);
			}
		}

		[FunctionName("Common_SFTP_DeleteFile")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPReadFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ColinSftpFile))]
		public async Task<IActionResult> deleteFile([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/sftp/deleteFile")] HttpRequestMessage req, ILogger log) {
			try {
				SFTPReadFileRequest readFile = await req.Content.ReadAsAsync<SFTPReadFileRequest>();

				var rc = new RestSharp.RestClient($"http://{config["opdgProxyHostname"]}:62001");
				var r = new RestSharp.RestRequest(@$"/api/sftp/deleteFile");
				r.AddJsonBody(readFile);
				var x = await rc.ExecutePostAsync<ColinSftpFile>(r);


				if (x.ErrorException != null)
					throw x.ErrorException;

				if (x.StatusCode == HttpStatusCode.OK) {
					return new OkObjectResult(x.Data);
				}

				throw new Exception(x.Content);

				//sftpReadFileRequest request = await System.Text.Json.JsonSerializer.DeserializeAsync<sftpReadFileRequest>(req.Body);

				//string password = await _kv.GetSecret(request.secretName);

				//var connectionInfo = new Renci.SshNet.ConnectionInfo(request.hostname, request.username, new PasswordAuthenticationMethod(request.username, password));
				//using (var client = new SftpClient(connectionInfo))
				//{
				//    client.Connect();

				//    var f = client.Get(request.filePath);

				//    f.Delete();

				//    return new OkObjectResult(new sftpFile()
				//    {
				//        Filename = f.Name,
				//        IsDirectory = f.IsDirectory,
				//        Fullname = f.FullName,
				//        Size = f.Length,
				//        Extension = Path.GetExtension(f.FullName),
				//        lastModified = f.LastWriteTimeUtc
				//    });

				//}
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex.Message);
			}
		}


		[FunctionName("Common_SFTP_CreateFile")]
		[OpenApiOperation]
		[OpenApiRequestBody("application/json", typeof(SFTPWriteFileRequest))]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ColinSftpFile))]
		public async Task<IActionResult> createFile([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "common/sftp/createFile")] HttpRequestMessage req, ILogger log) {
			try {
				SFTPListDirRequest readFile = await req.Content.ReadAsAsync<SFTPListDirRequest>();

				var rc = new RestSharp.RestClient($"http://{config["opdgProxyHostname"]}:62001");
				var r = new RestSharp.RestRequest(@$"/api/sftp/createFile");
				r.AddJsonBody(readFile);
				var x = await rc.ExecutePostAsync<ColinSftpFile>(r);

				if (x.ErrorException != null)
					throw x.ErrorException;


				if (x.StatusCode == HttpStatusCode.OK) {
					return new OkObjectResult(x.Data);
				}

				throw new Exception(x.Content);

				//sftpWriteFileRequest request = await System.Text.Json.JsonSerializer.DeserializeAsync<sftpWriteFileRequest>(req.Body);

				//string password = await _kv.GetSecret(request.secretName);

				//var connectionInfo = new Renci.SshNet.ConnectionInfo(request.hostname, request.username, new PasswordAuthenticationMethod(request.username, password));
				//using (var client = new SftpClient(connectionInfo))
				//{
				//    client.Connect();



				//    client.WriteAllBytes(request.filePath, request.fileContent);



				//    var f = client.Get(request.filePath);

				//    byte[] c = client.ReadAllBytes(request.filePath);

				//    using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
				//    {
				//        byte[] hashBytes = md5.ComputeHash(c);

				//        StringBuilder sb = new StringBuilder();
				//        for (int i = 0; i < hashBytes.Length; i++)
				//        {
				//            sb.Append(hashBytes[i].ToString("x2"));
				//        }

				//        return new OkObjectResult(new sftpFile()
				//        {
				//            Filename = f.Name,
				//            IsDirectory = f.IsDirectory,
				//            Fullname = f.FullName,
				//            Size = f.Length,
				//            Extension = Path.GetExtension(f.FullName),
				//            base64Content = Convert.ToBase64String(c),
				//            checksum = sb.ToString(),
				//            lastModified = f.LastWriteTimeUtc

				//        });
				//    }

				//}
			} catch (Exception ex) {
				return new BadRequestObjectResult(ex.Message);
			}
		}
	}
}