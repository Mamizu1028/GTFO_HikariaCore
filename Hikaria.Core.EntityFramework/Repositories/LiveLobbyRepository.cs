using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hikaria.Core.EntityFramework.Repositories
{
    public class LiveLobbyRepository : BaseRepository<LiveLobby>, ILiveLobbyRepository
    {
        public LiveLobbyRepository(GTFODbContext repositoryContext) : base(repositoryContext)
        {

        }

        public async Task UpdateLobbyDetailInfo(ulong lobbyID, DetailedLobbyInfo detailInfo)
        {
            var lobby = await FindByLobbyIDAsync(lobbyID);
            if (lobby != null)
            {
                lobby.DetailedInfo = detailInfo;
            }
        }

        public async Task UpdateLobbyStatusInfo(ulong lobbyID, LobbyStatusInfo statusInfo)
        {
            var lobby = await FindByLobbyIDAsync(lobbyID);
            if (lobby != null)
            {
                lobby.StatusInfo = statusInfo;
            }
        }

        public async Task UpdateLobbyPrivacySettings(ulong lobbyID, LobbyPrivacySettings privacySettings)
        {
            var lobby = await FindByLobbyIDAsync(lobbyID);
            if (lobby != null)
            {
                lobby.PrivacySettings = privacySettings;
            }
        }

        public async Task CreateOrUpdateLobby(LiveLobby lobby)
        {
            var dbLobby = await FindByLobbyIDAsync(lobby.LobbyID);
            if (dbLobby != null)
            {
                dbLobby.LobbyID = lobby.LobbyID;
                dbLobby.LobbyName = lobby.LobbyName;
                dbLobby.PrivacySettings = lobby.PrivacySettings;
                dbLobby.DetailedInfo = lobby.DetailedInfo;
            }
            else
            {
                Create(new LiveLobby(lobby.LobbyID, lobby.LobbyName, lobby.PrivacySettings, lobby.DetailedInfo));
            }
        }

        public async Task<IEnumerable<LiveLobby>> QueryLobby(LiveLobbyQueryBase filter)
        {
            if (filter.Privacy == LobbyPrivacy.Invisible)
                return new List<LiveLobby>().AsEnumerable();
            var lobbies = await _dbContext.LiveLobbies.Where(p => p.DetailedInfo.Revision == filter.Revision
                && p.PrivacySettings.Privacy == filter.Privacy
                && p.DetailedInfo.IsPlayingModded == filter.IsPlayingModded
                && (!filter.IgnoreFullLobby || p.DetailedInfo.OpenSlots > 0)).ToListAsync();
            var result = lobbies.Where(p =>
                (string.IsNullOrEmpty(filter.ExpeditionName) || p.DetailedInfo.ExpeditionName.Contains(filter.ExpeditionName, StringComparison.InvariantCultureIgnoreCase))
                && (string.IsNullOrEmpty(filter.Expedition) || p.DetailedInfo.Expedition.Contains(filter.Expedition, StringComparison.InvariantCultureIgnoreCase))
                && (string.IsNullOrEmpty(filter.LobbyName) || p.LobbyName.Contains(filter.LobbyName, StringComparison.InvariantCultureIgnoreCase))
                && (string.IsNullOrEmpty(filter.RegionName) || p.DetailedInfo.RegionName.Contains(filter.RegionName, StringComparison.InvariantCultureIgnoreCase))
            );
            return result;
        }

        public async Task<IReadOnlyDictionary<ulong, LiveLobby>> GetLobbyLookupNotTracking()
        {
            return await _dbContext.LiveLobbies.AsNoTracking().ToDictionaryAsync(p => p.LobbyID);
        }

        public async Task DeleteLobby(params ulong[] lobbyIDs)
        {
            var IDs = lobbyIDs.ToList();
            var lobbies = await _dbContext.LiveLobbies.Where(p => IDs.Contains(p.LobbyID)).ToListAsync();
            _dbContext.LiveLobbies.RemoveRange(lobbies);
            await _dbContext.SaveChangesAsync();
        }

        public async Task KeepLobbyAlive(ulong lobbyID)
        {
            var lobby = await FindByLobbyIDAsync(lobbyID);
            if (lobby != null)
            {
                lobby.ExpirationTime = DateTime.Now.AddSeconds(30);
            }
        }

        public async Task<LiveLobby?> FindByLobbyIDAsync(ulong lobbyID)
        {
            return await _dbContext.LiveLobbies.FindAsync(lobbyID);
        }

        public async Task DeleteExpiredLobbies()
        {
            var alllobbies = await _dbContext.LiveLobbies.Where(p => p.ExpirationTime < DateTime.Now).ToListAsync();
            _dbContext.LiveLobbies.RemoveRange(alllobbies);
            await _dbContext.SaveChangesAsync();
        }
    }
}
