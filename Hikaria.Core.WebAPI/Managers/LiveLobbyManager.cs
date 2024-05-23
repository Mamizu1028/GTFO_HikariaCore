using Hikaria.Core.Entities;

namespace Hikaria.Core.WebAPI.Managers
{
    public static class LiveLobbyManager
    {
        private static Dictionary<int, Dictionary<ulong, LiveLobby>> LiveLobbyLookup = new();

        private static bool TryGetLobby(int revision, ulong lobbyID, out LiveLobby lobby)
        {
            lobby = null;
            return LiveLobbyLookup.TryGetValue(revision, out var dic) && dic.TryGetValue(lobbyID, out lobby);
        }

        public static Task<bool> KeepLobbyAlive(int revision, ulong lobbyID)
        {
            if (TryGetLobby(revision, lobbyID, out var lobby))
            {
                lobby.KeepAlive();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public static Task<bool> UpdateLobbyDetailInfo(ulong lobbyID, DetailedLobbyInfo detailedInfo)
        {
            if (TryGetLobby(detailedInfo.Revision, lobbyID, out var lobby))
            {
                lobby.UpdateInfo(detailedInfo);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public static Task<bool> UpdateLobbyPrivacySettings(int revision, ulong lobbyID, LobbyPrivacySettings lobbySettings)
        {
            if (TryGetLobby(revision, lobbyID, out var lobby))
            {
                lobby.UpdatePrivacySettings(lobbySettings);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public static Task CreateLobby(LobbyIdentifier identifier, LobbyPrivacySettings lobbyPrivacySettings, DetailedLobbyInfo detailedLobbyInfo)
        {
            if (!LiveLobbyLookup.ContainsKey(detailedLobbyInfo.Revision))
            {
                LiveLobbyLookup.Add(detailedLobbyInfo.Revision, new());
            }
            LiveLobbyLookup[detailedLobbyInfo.Revision][identifier.ID] = new LiveLobby(identifier, lobbyPrivacySettings, detailedLobbyInfo);
            return Task.CompletedTask;
        }

        public static Task<IEnumerable<LiveLobby>> QueryLobby(LiveLobbyQueryBase filter)
        {
            if (!LiveLobbyLookup.ContainsKey(filter.Revision) || filter.Privacy == LobbyPrivacy.Invisible)
            {
                return Task.FromResult(new List<LiveLobby>().AsEnumerable());
            }
            return Task.FromResult(LiveLobbyLookup[filter.Revision].Values
                .TakeWhile(p => p.PrivacySettings.Privacy == filter.Privacy
                && p.DetailedInfo.IsPlayingModded == filter.IsPlayingModded
                && (filter.IgnoreFullLobby || p.DetailedInfo.OpenSlots > 0)
                && (string.IsNullOrEmpty(filter.ExpeditionName) || p.DetailedInfo.ExpeditionName.Contains(filter.ExpeditionName, StringComparison.InvariantCultureIgnoreCase))
                && (string.IsNullOrEmpty(filter.Expedition) || p.DetailedInfo.Expedition.Contains(filter.Expedition, StringComparison.InvariantCultureIgnoreCase))
                && (string.IsNullOrEmpty(filter.LobbyName) || p.Identifier.Name.Contains(filter.LobbyName, StringComparison.InvariantCultureIgnoreCase))
                && (string.IsNullOrEmpty(filter.RegionName) || p.DetailedInfo.RegionName.Contains(filter.RegionName, StringComparison.InvariantCultureIgnoreCase))));
        }

        public static Task<Dictionary<int, Dictionary<ulong, LiveLobby>>> GetLobbyLookup()
        {
            return Task.FromResult(LiveLobbyLookup);
        }

        public static void CheckLobbiesAlive()
        {
            HashSet<LiveLobby> expiredLobbies = new();
            foreach (var kvp in LiveLobbyLookup.Values)
            {
                foreach (var lobby in kvp.Values)
                {
                    if (lobby.ExpirationTime < DateTime.Now)
                    {
                        expiredLobbies.Add(lobby);
                    }
                }
            }
            foreach (var lobby in expiredLobbies)
            {
                LiveLobbyLookup[lobby.DetailedInfo.Revision].Remove(lobby.Identifier.ID);
            }
        }
    }
}
