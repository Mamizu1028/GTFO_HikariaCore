using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hikaria.Core.EntityFramework.Repositories
{
    public class BannedPlayersRepository : BaseRepository<BannedPlayer>, IBannedPlayerRepository
    {
        public BannedPlayersRepository(GTFODbContext repositoryContext) : base(repositoryContext)
        {
        }

        public async Task<List<BannedPlayer>> GetAllBannedPlayers()
        {
            return await FindAll().OrderBy(p => p.SteamID).ToListAsync();
        }

        public async Task BanPlayer(BannedPlayer player)
        {
            var dbPlayer = await _dbContext.BannedPlayers.FindAsync(player.SteamID);
            if (dbPlayer == null)
            {
                await _dbContext.BannedPlayers.AddAsync(player);
            }
            else
            {
                dbPlayer.DateBanned = DateTime.UtcNow;
            }
        }

        public async Task UnbanPlayer(ulong steamid)
        {
            var player = await _dbContext.BannedPlayers.FindAsync(steamid);
            if (player != null)
            {
                _dbContext.BannedPlayers.Remove(player);
            }
        }

        public async Task<BannedPlayer?> GetBannedPlayerBySteamID(ulong steamid)
        {
            return await FindByCondition(p => p.SteamID == steamid).FirstOrDefaultAsync();
        }
    }
}
