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

    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateLoggerInstance(nameof(HttpHelper));

    public static async Task<T> GetAsync<T>(string url) where T : new()
    {
        if (!CoreGlobal.ServerOnline) return new();
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (Exception ex)
        {
            Logger.Error("Error occurred while sending GET request");
            Logger.Exception(ex);
            return new();
        }
    }

    public static async Task<T> PostAsync<T>(string url, object content) where T : new()
    {
        if (!CoreGlobal.ServerOnline) return new();
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
            Logger.Error("Error occurred while sending POST request");
            Logger.Exception(ex);
            return new();
        }
    }

    public static async Task<T> PutAsync<T>(string url, object content) where T : new()
    {
        if (!CoreGlobal.ServerOnline) return new();
        try
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PutAsync(url, httpContent);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (Exception ex)
        {
            Logger.Error("Error occurred while sending PUT request");
            Logger.Exception(ex);
            return new();
        }
    }

    public static async Task<T> PatchAsync<T>(string url, object content) where T : new()
    {
        if (!CoreGlobal.ServerOnline) return new();
        try
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PatchAsync(url, httpContent);
            response.EnsureSuccessStatusCode();
            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }
        catch (Exception ex)
        {
            Logger.Error("Error occurred while sending PATCH request");
            Logger.Exception(ex);
            return new();
        }
    }
}
