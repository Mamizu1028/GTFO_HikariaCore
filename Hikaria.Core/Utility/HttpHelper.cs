using Clonesoft.Json;
using System.Text;
using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.Utility;

public static class HttpHelper
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    private static IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(HttpHelper));

    public static async Task<T> GetAsync<T>(string url) where T : new()
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData, CoreGlobal.JsonSerializerSettings);
        }
        catch (Exception)
        {
            _logger.Error($"Error occurred while sending GET request. [{url}]");
            return new();
        }
    }

    public static async Task<T> PostAsync<T>(string url, object content) where T : new()
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(content, CoreGlobal.JsonSerializerSettings);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(url, httpContent);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData, CoreGlobal.JsonSerializerSettings);
        }
        catch (Exception)
        {
            _logger.Error($"Error occurred while sending POST request. [{url}]");
            return new();
        }
    }

    public static async Task<T> PutAsync<T>(string url, object content) where T : new()
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(content, CoreGlobal.JsonSerializerSettings);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PutAsync(url, httpContent);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData, CoreGlobal.JsonSerializerSettings);
        }
        catch (Exception)
        {
            _logger.Error($"Error occurred while sending PUT request. [{url}]");
            return new();
        }
    }

    public static async Task<T> PatchAsync<T>(string url, object content) where T : new()
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(content, CoreGlobal.JsonSerializerSettings);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PatchAsync(url, httpContent);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData, CoreGlobal.JsonSerializerSettings);
        }
        catch (Exception)
        {
            _logger.Error($"Error occurred while sending PATCH request. [{url}]");
            return new();
        }
    }
}
