using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;

namespace Hikaria.Core.EntityFramework.Repositories
{
    public class SteamUserRepository : BaseRepository<SteamUser>, ISteamUserRepository
    {
        public SteamUserRepository(GTFODbContext repositoryContext) : base(repositoryContext)
        {
        }

        public async Task<SteamUser?> FindUser(ulong steamid)
        {
            return await GTFODbContext.Set<SteamUser>().FindAsync(steamid);
        }
    }
}
