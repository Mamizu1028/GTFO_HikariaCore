using Clonesoft.Json;
using System.Net.Http.Headers;
using System.Text;
using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.Utility;

public class HttpClientHelper : IDisposable
{
    private bool _disposed = false;
    private readonly HttpClient _httpClient;
    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateLoggerInstance(nameof(HttpClientHelper));


    public HttpClientHelper(string username, string password)
    {
        _httpClient = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public HttpClientHelper()
    {
        _httpClient = new HttpClient();
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
            Logger.Error("Error occurred while sending GET request");
            Logger.Exception(ex);
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
            Logger.Error("Error occurred while sending POST request");
            Logger.Exception(ex);
            return default(T);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
