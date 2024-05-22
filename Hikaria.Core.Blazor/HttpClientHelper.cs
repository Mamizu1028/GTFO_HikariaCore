using Newtonsoft.Json;
using System.Text;

namespace Hikaria.Core.Blazor;

public class HttpClientHelper
{
    private readonly HttpClient _httpClient;

    public HttpClientHelper(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<T> GetAsync<T>(string url)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (Exception ex)
        {
            return default(T);
        }
    }

    public async Task<T> PostAsync<T>(string url, object content)
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(url, httpContent);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (Exception ex)
        {
            return default(T);
        }
    }
}
