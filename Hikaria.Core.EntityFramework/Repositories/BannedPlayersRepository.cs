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

        public void BanPlayer(BannedPlayer player)
        {
            player.DateBanned = DateTime.UtcNow;
            Create(player);
        }

        public void UnbanPlayer(BannedPlayer player)
        {
            Delete(player);
        }
    }
}
