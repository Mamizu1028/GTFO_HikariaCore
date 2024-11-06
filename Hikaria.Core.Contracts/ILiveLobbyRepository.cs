using Hikaria.Core.Entities;

namespace Hikaria.Core.Contracts
{
    public interface ILiveLobbyRepository : IBaseRepository<LiveLobby>
    {
        Task<IReadOnlyDictionary<ulong, LiveLobby>> GetLobbyLookupNotTracking();

        Task<LiveLobby?> FindByLobbyIDAsync(ulong lobbyID);

        Task CreateOrUpdateLobby(LiveLobby lobby);
        Task UpdateLobbyDetailInfo(ulong lobbyID, DetailedLobbyInfo detailInfo);
        Task UpdateLobbyStatusInfo(ulong lobbyID, LobbyStatusInfo statusInfo);
        Task UpdateLobbyPrivacySettings(ulong lobbyID, LobbyPrivacySettings lobbySettings);
        Task<IEnumerable<LiveLobby>> QueryLobby(LiveLobbyQueryBase filter);
        Task DeleteExpiredLobbies();
        Task KeepLobbyAlive(ulong lobbyID);
    }
}
