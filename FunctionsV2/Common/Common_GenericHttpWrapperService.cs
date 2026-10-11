using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FunctionsV2.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace FunctionsV2.Common
{
    public class Common_GenericHttpWrapperService : IGenericHttpWrapperService, IDisposable
    {

        private bool _disposedValue;

        private readonly HttpClient _httpClient;

        public Common_GenericHttpWrapperService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TResponse> MakeHttpRequestAsync<TRequest, TResponse>(string url, HttpMethod httpMethod, ILogger log, TRequest requestBody = default, bool handleErrors = true)
        {
            var request = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(_httpClient.BaseAddress + url)
            };

            if (requestBody != null)
            {
                string json;
                if (requestBody is JObject || requestBody is JArray)
                {
                    json = requestBody.ToString();
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }
                else if(requestBody is Dictionary<string, string>)
                {
                    request.Content = new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)requestBody);
                }
                else
                {
                    json = JsonSerializer.Serialize(requestBody);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }
                
            }
            var response = await _httpClient.SendAsync(request);
            if (handleErrors && !response.IsSuccessStatusCode)
            {

                throw new ApplicationException("An error occurred with the call " + response.StatusCode + " - Error: " + await response.Content.ReadAsStringAsync());
            }
            var responseJson = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var responseData = JsonSerializer.Deserialize<TResponse>(responseJson, options);
            return responseData;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
