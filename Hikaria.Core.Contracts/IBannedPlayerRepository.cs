using Hikaria.Core.Entities;

namespace Hikaria.Core.Contracts
{
    public interface IBannedPlayerRepository : IBaseRepository<BannedPlayer>
    {
        Task<List<BannedPlayer>> GetAllBannedPlayers();
        Task BanPlayer(BannedPlayer player);
        Task UnbanPlayer(ulong steamid);
        Task<BannedPlayer?> GetBannedPlayerBySteamID(ulong steamid);
    }
}
