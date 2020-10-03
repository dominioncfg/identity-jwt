using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IdentityJWT.ClientMVC.Infra
{
    public static class Extensions
    {
        public static async Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string url, T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await client.PostAsync(url, content);
        }
       
        public static T Deserialize<T>(this string obj)
        {
            return JsonConvert.DeserializeObject<T>(obj);
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime ToUnixTime(this long unixTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
    }
}
