using Hikaria.Core.Contracts;
using Hikaria.Core.EntityFramework.Repositories;

namespace Hikaria.Core.EntityFramework
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly GTFODbContext _gtfoDbContext;
        private IBannedPlayerRepository _bannedPlayers;
        private ISteamUserRepository _steamUsers;

        public IBannedPlayerRepository BannedPlayers => _bannedPlayers ??= new BannedPlayersRepository(_gtfoDbContext);
        public ISteamUserRepository SteamUsers => _steamUsers ??= new SteamUserRepository(_gtfoDbContext);

        public RepositoryWrapper(GTFODbContext gtfoDbContext)
        {
            _gtfoDbContext = gtfoDbContext;
        }

        public Task<int> Save()
        {
            return _gtfoDbContext.SaveChangesAsync();
        }
    }
}
