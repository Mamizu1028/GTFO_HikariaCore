using Hikaria.Core.Entities;
using System.Collections.ObjectModel;

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

        public static Task KeepLobbyAlive(int revision, ulong lobbyID)
        {
            if (TryGetLobby(revision, lobbyID, out var lobby))
            {
                lobby.KeepAlive();
            }
            return Task.CompletedTask;
        }

        public static Task UpdateLobbyDetailInfo(ulong lobbyID, DetailedLobbyInfo detailedInfo)
        {
            if (TryGetLobby(detailedInfo.Revision, lobbyID, out var lobby))
            {
                lobby.UpdateInfo(detailedInfo);
            }
            return Task.CompletedTask;
        }

        public static Task<bool> CreateLobby(LobbyIdentifier identifier, LobbySettings settings, DetailedLobbyInfo detailedInfo)
        {
            if (!LiveLobbyLookup.ContainsKey(detailedInfo.Revision))
            {
                LiveLobbyLookup.Add(detailedInfo.Revision, new());
            }
            if (!LiveLobbyLookup[detailedInfo.Revision].TryGetValue(identifier.ID, out var lobby))
            {
                lobby = new LiveLobby(identifier, settings, detailedInfo);
                LiveLobbyLookup[detailedInfo.Revision][identifier.ID] = lobby;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public static Task<LiveLobby> GetLobby(LobbyIdentifier identifier, DetailedLobbyInfo detailedInfo)
        {
            TryGetLobby(detailedInfo.Revision, identifier.ID, out var lobby);
            return Task.FromResult(lobby);
        }

        public static Task<IEnumerable<LiveLobby>> GetAllLobbies(int revision, LobbyType lobbyType = LobbyType.Public)
        {
            if (!LiveLobbyLookup.ContainsKey(revision))
            {
                LiveLobbyLookup.Add(revision, new());
            }
            return Task.FromResult(LiveLobbyLookup[revision].Values.TakeWhile(p => p.Settings.LobbyType == lobbyType));
        }

        public static Task<Dictionary<int, Dictionary<ulong, LiveLobby>>> GetLobbiesLookup()
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
