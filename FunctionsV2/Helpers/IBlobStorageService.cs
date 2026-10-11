using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FunctionsV2.Helpers.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FunctionsV2.Helpers
{
    //Interface
    public interface IBlobStorageService
    {
        Task<string> RetrieveEntityFromBlobStorage(string containerName, string entityName);
        Task<Response<BlobContentInfo>> AddJsonEntityToBlobStorage(BlobRequest request);
    }

    //Service
    public class BlobStorageService : IBlobStorageService
    {
        private readonly IConfiguration _config;

        public BlobStorageService(IConfiguration config)
        {
            _config = config;
        }

        //Entity name must include the file type, i.e. 'TestFile.txt'
        public async Task<string> RetrieveEntityFromBlobStorage(string containerName, string entityName)
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(_config["storageAccountConnectionString"], containerName);
            BlobClient blob = blobContainerClient.GetBlobClient(entityName);

            if (!await blob.ExistsAsync())
                throw new FileNotFoundException($"Schema '{entityName}' not found. ");

            return blob.DownloadContentAsync().Result.Value.Content.ToString();
        }



        public async Task<Response<BlobContentInfo>> AddJsonEntityToBlobStorage(BlobRequest request) 
        {
            // Add extension to filename from request
            string fileName = $"{request.FileName}.json";
            
            // Initialise blob client and blob
            BlobContainerClient blobContainerClient = new BlobContainerClient(_config["storageAccountConnectionString"], request.Container);
            BlobClient blob = blobContainerClient.GetBlobClient(fileName);

            // Convert JSON string content to bytes
            var byteContent = System.Text.Encoding.UTF8.GetBytes(request.Content);
            
            // Set blob headers to define content type 
            var blobHeaders = new BlobHttpHeaders
            {
                ContentType = "application/json"
            };

            // Upload file content to blob
            Response<BlobContentInfo> response = await blob.UploadAsync(
                new MemoryStream(byteContent),
                blobHeaders);

            return response;
        }
    }
}
