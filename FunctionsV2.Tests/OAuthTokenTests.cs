using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using FunctionsV2.Common;
using FunctionsV2.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace FunctionsV2.Tests
{
    public class OAuthTokenTests
    {
        private readonly OAuthToken _OAuthTokenService;

        private readonly ILogger _logger = Substitute.For<ILogger>();

        private readonly ICache _cache = Substitute.For<ICache>();

        private readonly IKeyVault _keyVault = Substitute.For<IKeyVault>();

        private readonly IConfiguration _config = Substitute.For<IConfiguration>();

        private readonly IGenericHttpWrapperService _httpClient = Substitute.For<IGenericHttpWrapperService>();

        private IServiceCollection services = new ServiceCollection();

        public OAuthTokenTests()
        {
            services.AddHttpClient();

            _OAuthTokenService = new OAuthToken(_cache, _keyVault, _config, _httpClient);
        }

        [Fact]
        public async void ShouldReturnTokenFromCache()
        {
            // Arrange
            var tokenInCache = JWTMockGenerator.GenerateJWT();
            TokenReq tokenReq = new TokenReq()
            {
                AccessToken = tokenInCache,
                ExpiresIn = 1799,
                Scope = string.Empty
            };

            Faker<ICache.RedisRead> cacheRead = new Faker<ICache.RedisRead>()
                .RuleFor(x => x.key, f => f.Name.FirstName())
                .RuleFor(x => x.value, JToken.FromObject(tokenReq))
                .RuleFor(x => x.hasValue, true);

            var cacheReadRecord = cacheRead.Generate(1);

            Faker<IOAuthToken.Token> token = new Faker<IOAuthToken.Token>()
                .RuleFor(x => x.RedisRead, cacheReadRecord.First())
                .RuleFor(x => x.GrantType, "client_credentials");

            _cache.CacheRead(Arg.Any<ICache.RedisRead>()).Returns(cacheRead);

            // Act
            var tokenResponse = await _OAuthTokenService.CheckIfTokenExistsInCache(token, _logger);

            // Assert
            tokenResponse.Should().NotBeNull();

            tokenResponse.value.Should().BeEquivalentTo(JToken.FromObject(tokenReq));
        }

        [Fact]

        public async void ShouldReturnTrueIfCacheTokenHasExpired()
        {
            // Arrange
            var tokenInCache = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NDI4MzE4NDMsImlzcyI6IlNhbXBsZV9BdXRoX1NlcnZlciIsImF1ZCI6IlNhbXBsZV9BdXRoX1NlcnZlciJ9.qLJ40NdDBz1foHFy3hJVU3dpxCCJZukaekysqyLwb3M";
            TokenReq tokenReq = new TokenReq()
            {
                AccessToken = tokenInCache,
                ExpiresIn = 1799,
                Scope = string.Empty
            };

            Faker<ICache.RedisRead> cacheRead = new Faker<ICache.RedisRead>()
                .RuleFor(x => x.key, f => f.Name.FirstName())
                .RuleFor(x => x.value, JToken.FromObject(tokenReq))
                .RuleFor(x => x.hasValue, true);

            var cacheReadRecord = cacheRead.Generate(1);

            Faker<IOAuthToken.Token> token = new Faker<IOAuthToken.Token>()
                .RuleFor(x => x.RedisRead, cacheReadRecord.First())
                .RuleFor(x => x.GrantType, "client_credentials");

            _cache.CacheRead(Arg.Any<ICache.RedisRead>()).Returns(cacheRead);

            // Act
            var tokenResponse = await _OAuthTokenService.CheckIfTokenExistsInCache(token, _logger);

            var checkExpiry = _OAuthTokenService.CheckIfTokenExpired(tokenResponse, _logger);

            checkExpiry.Should().BeTrue();
        }

        [Fact]
        public async void ShouldGenerateNewTokenIfCacheTokenDoesntExist()
        {
            // Arrange
            Faker<ICache.RedisRead> cacheRead = new Faker<ICache.RedisRead>()
                .RuleFor(x => x.key, f => f.Name.FirstName())
                .RuleFor(x => x.hasValue, false);

            var cacheReadRecord = cacheRead.Generate(1);

            Faker<IOAuthToken.Token> token = new Faker<IOAuthToken.Token>()
                .RuleFor(x => x.RedisRead, cacheReadRecord.First())
                .RuleFor(x => x.GrantType, "client_credentials");

            ICache.RedisRead? cache = null;
            _cache.CacheRead(Arg.Any<ICache.RedisRead>()).Returns(cache);

            var newToken = JWTMockGenerator.GenerateJWT();
            TokenResponse tokenRsp = new TokenResponse()
            {
                AccessToken = newToken,
                ExpiresIn = 1799,
                Scope = string.Empty
            };

            _config["sapTokenUrl"].Returns("");

            // Act
            var tokenResponse = await _OAuthTokenService.CheckIfTokenExistsInCache(token, _logger);

            // Assert
            tokenResponse.Should().BeNull();

            // Act
            _httpClient.MakeHttpRequestAsync<Dictionary<string, string>, TokenResponse>(Arg.Any<string>(), HttpMethod.Post, 
                _logger, Arg.Any<Dictionary<string, string>>()).Returns(tokenRsp);
            var generatedToken = await _OAuthTokenService.GenerateNewToken(token, _logger);

            generatedToken.Should().BeEquivalentTo(JToken.FromObject(tokenRsp));

        }
    }
}
