using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Provision
{
    public interface IAzureRestClient
    {
        AzureRestClient Authenticate();
        HttpResponseMessage Get(string url);
        HttpResponseMessage Post(string url, HttpContent content);
        HttpResponseMessage Put(string url, HttpContent content);
        HttpResponseMessage Put(string url, JObject content);
    }

    public class AzureRestClient : IAzureRestClient
    {
        private HttpClient httpClient;
        private Token token;
        private IServicePrincipal principal;

        public AzureRestClient(IServicePrincipal principal)
        {
            this.principal = principal;
            httpClient = new HttpClient();
        }

        public HttpResponseMessage Put(string url, HttpContent content)
        {
            return httpClient.PutAsync(url, content).Result;
        }

        public HttpResponseMessage Put(string url, JObject content)
        {
            return Put(url, new StringContent(content.ToString(), Encoding.UTF8, "application/json"));
        }

        // public HttpResponseMessage Put(string url, object content)
        // {
        //     return Put(url, new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json"));
        // }

        public HttpResponseMessage Post(string url, HttpContent content)
        {
            return httpClient.PostAsync(url, content).Result;
        }

        public HttpResponseMessage Get(string url)
        {
            return httpClient.GetAsync(url).Result;
        }

        // https://blog.jongallant.com/2017/11/azure-rest-apis-postman/
        public AzureRestClient Authenticate()
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();
            fields.Add("client_id", principal.ClientId.ToString());
            fields.Add("client_secret", principal.ClientSecret.ToString());
            fields.Add("grant_type", "client_credentials");
            fields.Add("resource", "https://management.azure.com");
            var content = new FormUrlEncodedContent(fields);

            var response = httpClient.PostAsync($"https://login.microsoftonline.com/{principal.TenantId}/oauth2/token", content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            token = JsonSerializer.Deserialize<Token>(responseContent);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
            // httpClient.DefaultRequestHeaders
            //     .Accept
            //         .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return this;
        }

        internal class Token
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public string ExpiresIn { get; set; }

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; }
        }


    }
}