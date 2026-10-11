using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using AzureIPNetworks;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using System.Drawing;
using System.Reactive.Subjects;
using YamlDotNet.Core;
using System;

namespace FunctionsV2.Common
{
    public class Common_VerifyAzureIP
    {

        private readonly ICache _cache;

        public Common_VerifyAzureIP(ICache _cache)
        {
            this._cache = _cache;
        }


        public class VerifyAzureIPResponse
        {
            public bool allowed { get; set; }
            public string subnet { get; set; }
        }



        [FunctionName("Common_VerifyAzureIP")]
        [OpenApiOperation]
        [OpenApiParameter("tag", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiParameter("ipaddress", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(VerifyAzureIPResponse))]
        public async Task<IActionResult> VerifyAzureIP([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            string tagName = req.Query["tag"];
            System.Net.IPAddress ipaddress;

            if (!string.IsNullOrEmpty(tagName))
            {
                if (System.Net.IPAddress.TryParse(req.Query["ipaddress"], out ipaddress))
                {
                    // Extract the region from tagName (assuming format is "AzureCloud.<region>")
                    string[] tagParts = tagName.Split('.');
                    if (tagParts.Length != 2 || tagParts[0] != "AzureCloud")
                    {
                        return new BadRequestObjectResult("Invalid tag name format. Expected format: AzureCloud.<region>");
                    }

                    string region = tagParts[1]; 

                    string cacheKey = $"{tagName}_{ipaddress}";

                    ICache.RedisRead verifyCache = await _cache.CacheRead(new ICache.RedisRead() { key = cacheKey });
                    if (verifyCache.hasValue)
                    {
                        return new OkObjectResult(verifyCache.value.ToObject<VerifyAzureIPResponse>());
                    }
                   
                    bool isAzureIp = await AzureIPsProvider.Remote.IsAzureIpAsync(ipaddress);
                    if (isAzureIp)
                    {
                        var networks = await AzureIPsProvider.Remote.GetNetworksAsync(AzureCloud.Public,region: region);
                        foreach (var network in networks)
                        {
                           
                            if (network.BaseAddress.ToString().Equals(ipaddress.ToString()))
                            {
                                var x = new VerifyAzureIPResponse() { allowed = true, subnet = network.BaseAddress.ToString() };
                                await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = x, expiry = 60 * 60 * 24 });
                                return new OkObjectResult(x);
                            }
                        }

                        //var networks = await AzureIPsHelper.GetAzureCloudIpsAsync();
                        //var network = networks.Values.SingleOrDefault(n => n.Name == tagName);
                        //if (network != null)
                        //{
                        //    foreach (string ap in network.Properties.AddressPrefixes)
                        //    {
                        //        System.Net.IPNetwork ipnetwork = System.Net.IPNetwork.Parse(ap);
                        //        if (ipnetwork.Contains(ipaddress))
                        //        {
                        //            var x = new VerifyAzureIPResponse() { allowed = true, subnet = ap };
                        //            await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = x, expiry = 60 * 60 * 24 });
                        //            return new OkObjectResult(x);
                        //        }
                        //    }
                        //}
                    }
                }
            }
            return new OkObjectResult(new VerifyAzureIPResponse() { allowed = false });
        }






        [FunctionName("Common_VerifyAzureSubnet")]
        [OpenApiOperation]
        [OpenApiParameter("tag", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiParameter("subnet", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiParameter("ipaddress", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(VerifyAzureIPResponse))]
        public async Task<IActionResult> VerifyAzureSubnet([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            string tagName = req.Query["tag"];
            string subnet = req.Query["subnet"];
            System.Net.IPAddress ipaddress;

            if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(subnet))
            {
                if (System.Net.IPAddress.TryParse(req.Query["ipaddress"], out ipaddress))
                {
                    // Extract the region from tagName (assuming format is "AzureCloud.<region>")
                    string[] tagParts = tagName.Split('.');
                    if (tagParts.Length != 2 || tagParts[0] != "AzureCloud")
                    {
                        return new BadRequestObjectResult("Invalid tag name format. Expected format: AzureCloud.<region>");
                    }

                    string region = tagParts[1];

                    string cacheKey = $"{tagName}_{subnet}_{ipaddress}";

                    ICache.RedisRead verifyCache = await _cache.CacheRead(new ICache.RedisRead() { key = cacheKey });
                    if (verifyCache.hasValue)
                    {
                        return new OkObjectResult(verifyCache.value.ToObject<VerifyAzureIPResponse>());
                    }

                    //bool isAzureIp = await AzureIPsHelper.IsAzureIpAsync(ipaddress);
                    bool isAzureIp = await AzureIPsProvider.Remote.IsAzureIpAsync(ipaddress);
                    if (isAzureIp)
                    {

                        var networks = await AzureIPsProvider.Remote.GetNetworksAsync(AzureCloud.Public, region: region);

                        foreach (var network in networks)
                        {

                            if (network.BaseAddress.ToString().Equals(ipaddress.ToString()))
                            {
                                var x = new VerifyAzureIPResponse() { allowed = true, subnet = subnet };
                                await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = x, expiry = 60 * 60 * 24 });
                                return new OkObjectResult(x);
                            }
                        }

                        //var networks = await AzureIPsHelper.GetAzureCloudIpsAsync();
                        //var network = networks.Values.SingleOrDefault(n => n.Name == tagName);
                        //if (network != null)
                        //{


                        //    var range = network.Properties.AddressPrefixes.SingleOrDefault(r => r == subnet);
                        //    if (range != null)
                        //    {

                        //        System.Net.IPNetwork ipnetwork = System.Net.IPNetwork.Parse(subnet);
                        //        if (ipnetwork.Contains(ipaddress))
                        //        {

                        //            var x = new VerifyAzureIPResponse() { allowed = true, subnet = subnet };
                        //            await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = x, expiry = 60 * 60 * 24 });
                        //            return new OkObjectResult(x);

                        //        }
                        //    }
                        //}
                    }
                }
            }
            return new OkObjectResult(new { allowed = false });
        }

        [FunctionName("Common_GetAzureSubnet")]
        [OpenApiOperation]
        [OpenApiParameter("tag", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiParameter("subnet", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiParameter("ipaddress", Required = true, In = Microsoft.OpenApi.Models.ParameterLocation.Query)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(VerifyAzureIPResponse))]
        public async Task<IActionResult> GetAzureSubnet([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {

            System.Net.IPAddress ipaddress;



            if (System.Net.IPAddress.TryParse(req.Query["ipaddress"], out ipaddress))
            {
               

                string cacheKey = $"subnetname_{ipaddress}";

                ICache.RedisRead verifyCache = await _cache.CacheRead(new ICache.RedisRead() { key = cacheKey });
                if (verifyCache.hasValue)
                {
                    return new OkObjectResult(verifyCache.value.ToObject<VerifyAzureIPResponse>());
                }

                //bool isAzureIp = await AzureIPsHelper.IsAzureIpAsync(ipaddress);
                bool isAzureIp = await AzureIPsProvider.Remote.IsAzureIpAsync(ipaddress);
                if (isAzureIp)
                {

                    var networks = await AzureIPsProvider.Remote.GetServiceTagsAsync(AzureCloud.Public);
                    foreach (var network in networks)
                    {
                        foreach (var r in network.Properties.AddressPrefixes)
                        {
                            System.Net.IPNetwork ipnetwork = System.Net.IPNetwork.Parse(r.ToString());
                            if (ipnetwork.Contains(ipaddress))
                            {
                                var x = new VerifyAzureIPResponse() { subnet = network.Name };
                                await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = x, expiry = 60 * 60 * 24 });
                                return new OkObjectResult(x);
                            }
                        }
                    }

                    //var networks = await AzureIPsHelper.GetAzureCloudIpsAsync();



                    //foreach (var n in networks.Values)
                    //{
                    //    foreach (var r in n.Properties.AddressPrefixes)
                    //    {
                    //        System.Net.IPNetwork ipnetwork = System.Net.IPNetwork.Parse(r);
                    //        if (ipnetwork.Contains(ipaddress))
                    //        {

                    //            var x = new VerifyAzureIPResponse() { subnet = n.Name };
                    //            await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = x, expiry = 60 * 60 * 24 });
                    //            return new OkObjectResult(x);

                    //        }

                    //    }
                    //}




                }
            }


            return new OkObjectResult(new VerifyAzureIPResponse());
        }
    }
}