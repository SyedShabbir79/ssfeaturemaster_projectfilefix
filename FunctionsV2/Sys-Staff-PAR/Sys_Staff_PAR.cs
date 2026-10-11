using Azure.Identity;
using Azure.Storage.Blobs;
using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;


namespace FunctionV2.Sys_Staff_PAR
{
    
    public class Sys_Staff_PAR
    {
        private readonly IBlobStorageService _blobStorageService;

        public Sys_Staff_PAR(IBlobStorageService blobStorageService) 
        {
            _blobStorageService = blobStorageService;
        }

        public class getpublicationsResponse
        {
            public string centaur_id { get; set; }

            public List<publication> publications { get; set; }

            public class publication
            {
                public string pubDate { get; set; }
                public string title { get; set; }
                public string link { get; set; }
                public string description { get; set; }
            }
        }

        [FunctionName("Sys_Staff_PAR_GetPublications")]
        [OpenApiOperation]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(getpublicationsResponse))]
        [OpenApiParameter("centaur_id", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Required = true, Type = typeof(string))]
        public ActionResult Run([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {

            string centaur_id = req.Query["centaur_id"];

            List<getpublicationsResponse.publication> publications = new List<getpublicationsResponse.publication>();

            string url = $"http://centaur.reading.ac.uk/cgi/exportview/creators/{centaur_id}/RSS2/{centaur_id}.xml";
            System.Xml.XmlDocument document = new System.Xml.XmlDocument();
            document.Load(url);

            //await Task.Delay(0);

            XmlNodeList xmlPublications = document.SelectNodes("/*[local-name()='rss']/*[local-name()='channel']/*[local-name()='item']");

            Regex pubYear = new Regex("\\((?<pubYear>\\d{4})\\)");
            foreach (XmlNode item in xmlPublications)
            {
                getpublicationsResponse.publication publication = new getpublicationsResponse.publication()
                {
                    title = item["title"].InnerText.Trim(),
                    description = item["description"].InnerText.Trim(),
                    link = item["link"].InnerText,
                    pubDate = "1900"
                };
                if (pubYear.IsMatch(publication.description))
                {
                    publication.pubDate = pubYear.Match(publication.description).Groups["pubYear"].Value;
                }
                publications.Add(publication);
            }

            return new OkObjectResult(new getpublicationsResponse() { centaur_id = centaur_id, publications = publications.OrderByDescending(x => x.pubDate).ToList() });
        }



        /// <summary>
        /// Duncan Taylor - 05/12/2025
        /// Helper method to fetch publication data (xml) from blob storage, parse and clean and return to client as ordered list
        /// Expects an HTTP GET request with query params centaur_id and blob_container, no body.
        /// </summary>

        [FunctionName("Sys_Staff_PAR_ParsePublications")]
        [OpenApiOperation]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(BaseFunctionResponse))]
        [OpenApiParameter("employee_number", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter("centaur_id", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter("blob_container", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Required = true, Type = typeof(string))]
        public async Task<IActionResult> ParsePublications([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {

            log.LogInformation("HTTP function Sys_Staff_PAR_ParsePublications invoked");

            // get centaur_id from query param
            string employeeNumber = req.Query["employee_number"];
            if (String.IsNullOrWhiteSpace(employeeNumber))
                return new BadRequestObjectResult(
                    new BaseFunctionResponse
                    {
                        Error = new BaseFunctionResponseError { Code = $"{((int)ErrorCodes.BadRequest)}", Message = "Missing employee_number query parameter" }
                    });

            // get centaur_id from query param
            string centaurId = req.Query["centaur_id"];
            if (String.IsNullOrWhiteSpace(centaurId))
                return new BadRequestObjectResult(
                    new BaseFunctionResponse
                    {
                        Error = new BaseFunctionResponseError { Code = $"{((int)ErrorCodes.BadRequest)}", Message = "Missing centaur_id query parameter" }
                    });

            // get blob location from query param
            string blobContainer = req.Query["blob_container"];
            if (String.IsNullOrWhiteSpace(blobContainer))
                return new BadRequestObjectResult(
                    new BaseFunctionResponse
                    {
                        Error = new BaseFunctionResponseError { Code = $"{((int)ErrorCodes.BadRequest)}", Message = "Missing blob_container query parameter" }
                    });
            
            // init. filename
            string fileName = $"{employeeNumber}.xml";

            log.LogInformation($"Connecting to Blob storage. Attempting to fetch data from {blobContainer}/{fileName}");

            List<getpublicationsResponse.publication> publications = new();

            // initialize XML Document
            XmlDocument document = new();
            XmlNodeList xmlPublications = document.SelectNodes("/*[local-name()='rss']/*[local-name()='channel']/*[local-name()='item']");

            try
            {
                // get centaur data from blob storage
                string centaurContent = await _blobStorageService.RetrieveEntityFromBlobStorage(blobContainer, fileName);

                //**** when testing locally, comment out _blobStorageService invokation above and uncomment code below ***
                /*
                BlobServiceClient blobServiceClient = new BlobServiceClient(
                new Uri("https://ukscommond001st.blob.core.windows.net"),
                new DefaultAzureCredential());

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(blobContainer);

                BlobClient blobClient = containerClient.GetBlobClient(fileName);
                if (!await blobClient.ExistsAsync())
                {
                    return new BadRequestObjectResult(
                        new BaseFunctionResponse
                        {
                            Error = new BaseFunctionResponseError { Code = $"{((int)ErrorCodes.NotFound)}", Message = $"Blob file {fileName} not found" }
                        });
                }

                using Stream blobStream = await blobClient.OpenReadAsync();
                using StreamReader streamReader = new(blobStream);
                string centaurContent = await streamReader.ReadToEndAsync();
                */
                //****

                // read centaur data into XML Document
                document.LoadXml(centaurContent);

                // parse xml, extract publications and append to list
                Regex pubYear = new Regex("\\((?<pubYear>\\d{4})\\)");
                foreach (XmlNode item in xmlPublications)
                {
                    getpublicationsResponse.publication publication = new getpublicationsResponse.publication()
                    {
                        title = item["title"].InnerText.Trim(),
                        description = item["description"].InnerText.Trim(),
                        link = item["link"].InnerText,
                        pubDate = "1900"
                    };
                    if (pubYear.IsMatch(publication.description))
                    {
                        publication.pubDate = pubYear.Match(publication.description).Groups["pubYear"].Value;
                    }
                    publications.Add(publication);
                }

                log.LogInformation($"HTTP function Sys_Staff_PAR_ParsePublications completed, returning {publications.Count} publications");

                // return orderded list of publications
                return new OkObjectResult(
                    new BaseFunctionResponse()
                    {
                        Response = new getpublicationsResponse 
                        { 
                            centaur_id = centaurId,
                            publications = publications.OrderByDescending(x => x.pubDate).ToList()
                        }
                    });
            }
            catch (Exception ex)
            {
                log.LogError($"Error - {ex}");                
                return new BadRequestObjectResult(
                  new BaseFunctionResponse
                  {
                      Error = new BaseFunctionResponseError 
                      { 
                          Code = $"{((int)ErrorCodes.ServerError)}", 
                          Message = $"Exception thrown while parsing publications for employee number {employeeNumber}. Exception: {ex}" }
                      }
                  );
            }
        }
    }
}
