using System.Threading.Tasks;

namespace IdentityJWT.ClientMVC.Services.HttpClientService
{
    public interface IHttpClientService
    {
        public Task<T> GetAsync<T>(string url, bool requireAuth = false) where T : class;
    }
}
