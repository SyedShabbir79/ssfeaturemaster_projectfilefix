using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FunctionsV2.Helpers
{
    public interface IGenericHttpWrapperService
    {
        public Task<TResponse> MakeHttpRequestAsync<TRequest, TResponse>(string url, HttpMethod httpMethod, ILogger log, TRequest requestBody =
            default, bool handleErrors = true);

    }
}
