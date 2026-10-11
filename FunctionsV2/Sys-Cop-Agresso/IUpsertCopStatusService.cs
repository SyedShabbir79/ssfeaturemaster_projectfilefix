using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Sys_Cop_Agresso.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FunctionsV2.Sys_Cop_Agresso
{
    //Interface
    public interface IUpsertCopStatusService
    {
        public BaseFunctionResponse UpsertCopStatus(CoPRequest request);
    }

    //Service
    public class UpsertCopStatusService : IUpsertCopStatusService
    {

        private readonly IConfiguration _config;
        private readonly IKeyVault _keyVault;
        private readonly IJwsService _jwsService;
        private readonly IHttpClientFactory _httpClientFactory;

        public UpsertCopStatusService(IConfiguration config,
            IKeyVault keyVault, IJwsService jwsService, IHttpClientFactory httpClientFactory)
        {
            this._config = config;
            _keyVault = keyVault;
            _jwsService = jwsService;
            _httpClientFactory = httpClientFactory;
        }

        public BaseFunctionResponse UpsertCopStatus(CoPRequest request)
        {
            try
            {
                //Validate that supplierId is present, else return error
                if(request.SupplierId == null)
                {
                    return new BaseFunctionResponse
                    {
                        Error = new BaseFunctionResponseError
                        {
                            Code = $"{(int)ErrorCodes.BadRequest}",
                            Message = $"No SupplierID has been provided."
                        }
                    };
                }

                //Get certificates from KeyVault
                X509Certificate2 tlsCert = _keyVault.GetCertificate(_config["lbgTlsCertName"]);
                X509Certificate2 mlsCert = _keyVault.GetCertificate(_config["lbgMlsCertName"]);

                if(tlsCert == null || mlsCert == null)
                {
                    string message = tlsCert == null && mlsCert == null ? "TLS & MLS"
                        : mlsCert == null ? "MLS"
                        : tlsCert == null ? "TLS"
                        : null;

                    return new BaseFunctionResponse
                    {
                        Error = new BaseFunctionResponseError
                        {
                            Code = $"{(int)ErrorCodes.BadRequest}",
                            Message = $"Could not retrieve certificate: {message}"
                        }
                    };
                }

                //Construct LBG CoP Headers
                CoPHeaders headers = ComposeHeaders(mlsCert, request.lbgRequest);

                //Call LBG CoP API
                HttpResponseMessage copResponse = CallCoPApi(tlsCert, mlsCert, request.lbgRequest, headers);
                string response = JsonConvert.SerializeObject(copResponse.Content.ReadAsStringAsync().Result);

                //Validate API response from LBG
                if (!copResponse.IsSuccessStatusCode)
                {
                    return new BaseFunctionResponse
                    {
                        Error = new BaseFunctionResponseError
                        {
                            Code = $"{(int)copResponse.StatusCode}",
                            Message = $"LBG - {ConstructErrorMessage(copResponse)}"
                        }
                    };
                }

                //Map successful LBG response to object
                CoPResponse copResponseObj = JsonConvert.DeserializeObject<CoPResponse>(copResponse.Content.ReadAsStringAsync().Result);

                //Transform response into Agresso request schema
                List<AgressoCopRequest> coPSupplier = TransformCopResponseToAgressoRequest(copResponseObj);

                //Call Agresso CoP API
                HttpResponseMessage agressoResponse = CallAgressoCopAPI(coPSupplier, request.SupplierId);
                //HttpResponseMessage agressoResponse = CallAgressoUpsertCopStatus(agressoUsername, agressoPassword, coPSupplier, supplierId.Replace(",", ""));
                string test = agressoResponse.Content.ReadAsStringAsync().Result;

                if (!agressoResponse.IsSuccessStatusCode)
                {
                    return new BaseFunctionResponse
                    {
                        Error = new BaseFunctionResponseError
                        {
                            Code = $"{(int)agressoResponse.StatusCode}",
                            Message = $"Agresso - {ConstructErrorMessage(agressoResponse)}"
                            
                        }
                    };
                }

                return new BaseFunctionResponse
                {
                    Response = new
                    {
                        Result = new
                        {
                            SupplierId = request.SupplierId,
                            cop_response_fx = coPSupplier.FirstOrDefault().value.copresponse.cop_response_fx,
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseFunctionResponse
                {
                    Error = new BaseFunctionResponseError
                    {
                        Code = $"{((int)ErrorCodes.Unknown)}",
                        Message = $"Message - {ex.Message}, Stack Trace - {ex.StackTrace}"
                    }
                };
            }
        }                 

        private string ConstructErrorMessage(HttpResponseMessage responseMessage)
        {
            return responseMessage.Content.ReadAsStringAsync().Result != string.Empty ? responseMessage.Content.ReadAsStringAsync().Result
                        : responseMessage.ReasonPhrase;
        }

        private CoPHeaders ComposeHeaders(X509Certificate2 mlsCertificate, LbgRequest request)
        {
            string subKey = _keyVault.GetSecret("uor-lbg-cop-subscriptionKey").Result;
            string clientId = _keyVault.GetSecret("uor-lbg-cop-clientId").Result;
            string keyId = _keyVault.GetSecret("uor-lbg-cop-keyId").Result;

            JwtRequest jwtRequest = new JwtRequest
            {
                Certificate = mlsCertificate,
                Payload = ConvertPayloadToDictionary(request),
                KeyId = keyId,
            };

            string jwsToken = _jwsService.GenerateLbgJws(jwtRequest);
            string[] jwsParts = jwsToken.Split('.');

            return new CoPHeaders
            { 
                SubscriptionKey = subKey,
                ClientId = clientId,
                JwsToken = jwsParts[0] + ".." + jwsParts[2]
            };
        }

        private Dictionary<string, object> ConvertPayloadToDictionary(LbgRequest request)
        {
            Dictionary<string, object> payloadData = new()
            {
                { "sortCode", request.account.sortCode },
                { "accountNumber", request.account.accountNumber },
                { "name", request.account.name },
                { "accountType", request.account.accountType },
                { "identifier", request.account.identifier }
            };

            return new()
            {
                { "account", payloadData }
            };
        }

        private HttpResponseMessage CallCoPApi(X509Certificate2 tlsCert, X509Certificate2 mlsCert, LbgRequest request, CoPHeaders headers)
        {
            HttpClientHandler handler = new();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = SslProtocols.Tls12;
            handler.ClientCertificates.Add(new X509Certificate2(tlsCert));

            using (HttpClient httpClient = new(handler))
            {
                httpClient.BaseAddress = new Uri($"{_config["lbgCoPUri"]}");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", headers.SubscriptionKey);
                httpClient.DefaultRequestHeaders.Add("x-lbg-origin-client-id", headers.ClientId);
                httpClient.DefaultRequestHeaders.Add("x-jws-signature", headers.JwsToken);
                httpClient.DefaultRequestHeaders.Add("x-lbg-txn-correlation-id", Guid.NewGuid().ToString());

                StringContent stringContent = new(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                return httpClient.PostAsync(httpClient.BaseAddress, stringContent).Result;
            }
        }

        private List<AgressoCopRequest> TransformCopResponseToAgressoRequest(CoPResponse copResponse)
        {
           return new List<AgressoCopRequest>
           {
               new AgressoCopRequest{
                   path = "customFieldGroups",
                   op = "Replace",
                   value = new Value
                   {
                       copresponse = new CoPResponseFx
                       {
                           cop_response_fx = $"{copResponse.matched} - {copResponse.reasonCode} - {copResponse.name}"
                       }
                   },
                   supportNestedCollections = true
               }
           };
        }

        private HttpResponseMessage CallAgressoCopAPI(List<AgressoCopRequest> agressoRequest, string supplierId)
        {
            string jsonRequest = JsonConvert.SerializeObject(agressoRequest);
            JToken requestBody = JToken.Parse(JsonConvert.SerializeObject(agressoRequest));

            // Construct target API URL
            string internalUrl = $"{_config["agressoCopServices"]}/{supplierId}";

            // Retrieve secrets
            string agressoUserName = _keyVault.GetSecret("UoR-Common-Agresso-UserName").Result;
            string agressoPassword = _keyVault.GetSecret("UoR-Common-Agresso-Password").Result;

            using (var client = _httpClientFactory.CreateClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{agressoUserName}:{agressoPassword}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var method = new HttpMethod("PATCH");
                var request = new HttpRequestMessage(method, internalUrl)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json-patch+json")
                };
                return client.SendAsync(request).Result; 
            }           
        }
    }
}
