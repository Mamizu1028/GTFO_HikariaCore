using Hikaria.Core.Entities;

namespace Hikaria.Core.Contracts
{
    public interface ISteamUserRepository : IBaseRepository<SteamUser>
    {
        Task<SteamUser?> FindUser(ulong steamid);
    }
}
