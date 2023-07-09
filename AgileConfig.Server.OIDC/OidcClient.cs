﻿using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace AgileConfig.Server.OIDC
{
    public class OidcClient
    {
        private readonly OidcSetting _oidcSetting;

        public OidcClient(OidcSetting oidcSetting)
        {
            _oidcSetting = oidcSetting;
        }

        public async Task<TokenModel> Validate(string code)
        {

            var httpclient = new HttpClient();
            var kvs = new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", _oidcSetting.RedirectUri),
                new KeyValuePair<string, string>("client_id", _oidcSetting.ClientId),
                new KeyValuePair<string, string>("client_secret", _oidcSetting.ClientSecret),
            };
            var form = new FormUrlEncodedContent(kvs);
            var response = await httpclient.PostAsync(_oidcSetting.TokenEndpoint, form);
            response.EnsureSuccessStatusCode();
            var bodyJson = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(bodyJson))
            {
                throw new Exception("Can not validate the code. The token endpoint return the empty response.");
            }

            dynamic responseObject = JsonConvert.DeserializeObject<dynamic>(bodyJson);
            string access_token = responseObject.access_token;
            string id_token = responseObject.id_token;

            if (string.IsNullOrWhiteSpace(access_token) || string.IsNullOrWhiteSpace(id_token))
            {
                throw new Exception("Can not validate the code. Access token or Id token missing.");
            }

            var obj = new TokenModel();
            obj.IdToken = id_token;
            obj.AccessToken = access_token;

            return obj;
        }

        public (string Id, string UserName) UnboxIdToken(string idToken)
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
            var id = jwtToken.Claims.Where(x=>x.Type  == "sub").FirstOrDefault()?.Value;
            var userName = jwtToken.Claims.Where(x => x.Type == "email").FirstOrDefault()?.Value;

            return (id, userName);
        }
    }
    
    public class TokenModel
    {
        public string IdToken { get; set;}

        public string AccessToken { get; set;}
    }
}