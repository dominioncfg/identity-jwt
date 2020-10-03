using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System;
using IdentityJWT.ClientMVC.Infra;
using System.Threading.Tasks;

namespace IdentityJWT.ClientMVC.Services.HttpClientService
{
    public class JwtSecuredHttpClientService : IHttpClientService
    {
        IHttpContextAccessor _httpContextAccessor;
        IHttpClientFactory _clientFactory;

        public JwtSecuredHttpClientService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
        }

        public async Task<T> GetAsync<T>(string url, bool requireAuth = true) where T : class
        {
            var client = _clientFactory.CreateClient();
            string token = _httpContextAccessor.HttpContext.Session.GetString("user_token.Access");
            if (string.IsNullOrEmpty(token) && requireAuth)
            {
                throw new Exception("You are not ");
            }
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            if (requireAuth)
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            }
            var response = await client.GetAsync(url);
            
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await response.Content.ReadAsStringAsync();
                return content.Deserialize<T>();
            }
            else
            {
                throw new Exception("Request has failed");
            }
        }
    }
}
