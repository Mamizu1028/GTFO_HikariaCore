using Hikaria.Core.Entities;
using Microsoft.AspNetCore.Components;

namespace Hikaria.Core.Blazor.Pages
{
    public partial class BannedPlayers
    {
        private static Dictionary<ulong, BannedPlayer> _bannedPlayersLookup = new();
        private HttpClientHelper _httpClientHelper;
        private HttpClientHelper HttpClientHelper => _httpClientHelper ??= new(_httpClient);
        [Inject]
        private HttpClient _httpClient { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var result = await HttpClientHelper.GetAsync<List<BannedPlayer>>("https://q1w2e3r4t5y6u7i8o9p0.top:50001/api/gtfo/BannedPlayers/GetAllBannedPlayers");
            _bannedPlayersLookup = result.ToDictionary(p => p.SteamID);
            StateHasChanged();
        }
    }
}
