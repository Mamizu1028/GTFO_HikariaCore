
using Hikaria.Core.Entities;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Hikaria.Core.Blazor.Pages
{
    public partial class LiveLobbies
    {
        private static Dictionary<int, Dictionary<ulong, LiveLobby>> LiveLobbyLookup = new();
        private static List<LiveLobby> LiveLobbyList = new();
        private static QueryRequest queryRequest = new() { Revision = -1, LobbyType = LobbyType.Public };
        private class QueryRequest
        {
            public int Revision { get; set; }
            public LobbyType LobbyType { get; set; }
        }

        private HttpClientHelper _httpClientHelper;
        private HttpClientHelper HttpClientHelper => _httpClientHelper ??= new(_httpClient);
        [Inject]
        private HttpClient _httpClient { get; set; }

        protected override async Task OnInitializedAsync()
        {
            LiveLobbyLookup = await HttpClientHelper.GetAsync<Dictionary<int, Dictionary<ulong, LiveLobby>>>($"https://q1w2e3r4t5y6u7i8o9p0.top:50001/api/gtfo/LiveLobby/GetLobbiesLookup");
            StateHasChanged();
        }

        private void OnQuery()
        {
            LiveLobbyList.Clear();
            if (queryRequest.Revision == -1)
            {
                LiveLobbyLookup.Clear();
                foreach (var dic in LiveLobbyLookup.Values)
                {
                    foreach (var lobby in dic.Values)
                    {
                        if (lobby.Settings.LobbyType == queryRequest.LobbyType)
                        {
                            LiveLobbyList.Add(lobby);
                        }
                    }
                }
            }
            else if (LiveLobbyLookup.TryGetValue(queryRequest.Revision, out var dic))
            {
                foreach (var lobby in dic.Values)
                {
                    if (lobby.Settings.LobbyType == queryRequest.LobbyType)
                    {
                        LiveLobbyList.Add(lobby);
                    }
                }
            }
            StateHasChanged();
        }
    }
}
