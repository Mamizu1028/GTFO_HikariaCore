namespace Hikaria.Core.Contracts
{
    public interface IRepositoryWrapper
    {
        ISteamUserRepository SteamUsers { get; }
        IBannedPlayerRepository BannedPlayers { get; }
        ILiveLobbyRepository LiveLobbies { get; }
        Task<int> Save();
    }
}
