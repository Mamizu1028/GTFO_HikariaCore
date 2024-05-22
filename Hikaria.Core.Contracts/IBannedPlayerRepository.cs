using Hikaria.Core.Entities;

namespace Hikaria.Core.Contracts
{
    public interface IBannedPlayerRepository : IBaseRepository<BannedPlayer>
    {
        Task<List<BannedPlayer>> GetAllBannedPlayers();
        void BanPlayer(BannedPlayer player);
        void UnbanPlayer(BannedPlayer player);
    }
}
