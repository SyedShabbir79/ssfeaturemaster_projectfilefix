using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace FunctionsV2.Helpers
{
    public interface IOAuthToken
    {
        public class Token
        {
            public ICache.RedisRead RedisRead { get; set; }
            public string GrantType { get; set; }
        }

        public Task<ICache.RedisRead> CheckIfTokenExistsInCache(IOAuthToken.Token token, ILogger log);

        public bool CheckIfTokenExpired(ICache.RedisRead redisRead, ILogger log);

        public Task<JToken> GenerateNewToken(IOAuthToken.Token token, ILogger log);
    }

    public class OAuthToken : IOAuthToken
    {
        private readonly ICache _cache;
        private readonly IKeyVault _keyVault;
        private readonly IConfiguration _config;
        private readonly IGenericHttpWrapperService _httpClient;

        public OAuthToken(ICache cache, IKeyVault keyvault, IConfiguration config, IGenericHttpWrapperService httpClient)
        {
            _cache = cache;
            _keyVault = keyvault;
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<ICache.RedisRead> CheckIfTokenExistsInCache(IOAuthToken.Token token, ILogger log)
        {
            // Retrieve token from redis cache
            ICache.RedisRead redisRead;
            log.LogInformation("Common_GetOAuthToken - Check if RedisRead.Key is present in the input message");
            if (token.RedisRead != null && token.RedisRead.key != null && !string.IsNullOrEmpty(token.RedisRead.key))
            {
                log.LogInformation("Common_GetOAuthToken - read the token from cache");
                redisRead = await _cache.CacheRead(token.RedisRead);
            }
            else
            {
                throw new InvalidOperationException("Key cannot be null/empty");
            }

            log.LogInformation("Common_GetOAuthToken - check if the token is present in cache");
            return redisRead;
        }

        public bool CheckIfTokenExpired(ICache.RedisRead redisRead, ILogger log)
        {
            // Check if token is not yet expired
            log.LogInformation("Common_GetOAuthToken - Deserialize the RedisRead.value into the TokenReq object");
            var tokenFromCache = JsonSerializer.Deserialize<TokenReq>(redisRead.value.ToString());

            log.LogInformation("Common_GetOAuthToken - get the token expiry timestamp");
            var tokenDt = GetExpiryTimestamp(tokenFromCache.AccessToken);

            log.LogInformation("Common_GetOAuthToken - check if the token is not yet expired");
            if (tokenDt > DateTime.Now)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        
        public async Task<JToken> GenerateNewToken(IOAuthToken.Token token, ILogger log)
        {
            string systemName = token.RedisRead.key.ToString();
            string clientId = string.Empty;
            string clientSecret = string.Empty;
            string tokenEndpoint = string.Empty;
            string userName = string.Empty;
            string password = string.Empty;

            clientId = await _keyVault.GetSecret(systemName.ToLower() + "ClientId");
            clientSecret = await _keyVault.GetSecret(systemName.ToLower() + "ClientSecret");
            if (token.GrantType == "password")
            {
                userName = await _keyVault.GetSecret(systemName.ToLower() + "UserName");
                password = await _keyVault.GetSecret(systemName.ToLower() + "Password");
            }

            tokenEndpoint = _config[systemName.ToLower() + "TokenUrl"];

            var response = await GetAccessTokenAsync(token.GrantType, clientId, clientSecret, userName, password, tokenEndpoint, log);

            TokenReq tokenRsp = new TokenReq()
            {
                AccessToken = response.AccessToken,
                ExpiresIn = response.ExpiresIn,
                Scope = response.Scope
            };

            // Write to Redis cache
            await WriteToRedisCache(systemName, tokenRsp);


            return JToken.FromObject(tokenRsp);
        }

        private DateTime GetExpiryTimestamp(string accessToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                    return DateTime.MinValue;
                if (!accessToken.Contains("."))
                    return DateTime.MinValue;

                string[] parts = accessToken.Split('.');
                MyJwtToken payload = JsonSerializer.Deserialize<MyJwtToken>(Convert.FromBase64String(parts[1]));
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(payload.exp);
                return dateTimeOffset.LocalDateTime;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        private async Task WriteToRedisCache(string systemName, TokenReq token)
        {
            ICache.RedisWrite redisWrite = new ICache.RedisWrite()
            {
                key = systemName,
                value = token,
                expiry = token.ExpiresIn
            };

            redisWrite = await _cache.CacheWrite(redisWrite);
        }


        public async Task<TokenResponse> GetAccessTokenAsync(string grantType, string clientId, string clientSecret, string username, string password, string tokenEndpoint, ILogger log)
        {

            Dictionary<string, string> requestData = null;

            if (grantType == "password")
            {
                // Prepare the request body
                requestData = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "username", username },
                    { "password", password }
                };
            }
            else if (grantType == "client_credentials")
            {
                // Prepare the request body
                requestData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                };
            }
            else
                throw new Exception("Invalid granttype received");


            // Send POST request to get the access token
            var response = await _httpClient.MakeHttpRequestAsync<Dictionary<string, string>, TokenResponse>(
                tokenEndpoint, HttpMethod.Post, log, requestData);


            return response;  
        }
    }

    public class MyJwtToken
    {
        public long exp { get; set; }
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")]
        public string Scope { get; set; }
    }

    public class TokenReq
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string Scope { get; set; }
    }
}
