using FunctionsV2.Helpers.Models;
using JWT;
using JWT.Algorithms;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace FunctionsV2.Helpers
{
    //Interface
    public interface IJwsService
    {
        string GenerateLbgJws(JwtRequest jwtRequest);
    }

    //Service
    public class JwsService : IJwsService
    {
        public JwsService() 
        {
            
        }

        //Lloyds Banking Group generates their JWS tokens in a bespoke way
        public string GenerateLbgJws(JwtRequest request)
        {
            Dictionary<string, object> headers = new()
            {
                { "kid", request.KeyId }
            };

            if (!request.Certificate.HasPrivateKey)
            {
                throw new ArgumentException("Certificate does not contain a private key.");
            }

            // Extract RSA private key from certificate
            var rsa = request.Certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException("Could not extract RSA private key from certificate.");
            }

            // JWT setup
            var serializer = new JWT.Serializers.JsonNetSerializer();
            var urlEncoder = new JwtBase64UrlEncoder();
            var algorithm = new RS256Algorithm(rsa, rsa);
            var encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

            return encoder.Encode(headers, request.Payload, new byte[0]);
        }


        private string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
