namespace Hikaria.Core.Contracts
{
    public interface IRepositoryWrapper
    {
        ISteamUserRepository SteamUsers { get; }
        IBannedPlayerRepository BannedPlayers { get; }
        Task<int> Save();
    }
}
