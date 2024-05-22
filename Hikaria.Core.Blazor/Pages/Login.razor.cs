
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Hikaria.Core.Blazor.Pages
{
    public partial class Login
    {
        private HttpClientHelper _httpClientHelper;
        private HttpClientHelper HttpClientHelper => _httpClientHelper ??= new(_httpClient);
        [Inject] private HttpClient _httpClient { get; set; }
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }

        private LoginRequest loginRequest = new() { SteamID = 0UL, Password = string.Empty };

        private class LoginRequest
        {
            public ulong SteamID { get; set; }
            public string Password { get; set; }
        }
        
        public async Task OnLogin()
        {
            var result = await HttpClientHelper.PostAsync<SampleResult>($"http://q1w2e3r4t5y6u7i8o9p0.top:50001/api/gtfo/Auth/Login", loginRequest);
        }

        private class SampleResult
        {
            public string Message { get; set; }
            public string Token { get; set; }
        }
    }
}
