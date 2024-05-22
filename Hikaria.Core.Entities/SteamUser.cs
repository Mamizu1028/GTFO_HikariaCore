namespace Hikaria.Core.Entities
{
    public class SteamUser
    {
        public ulong SteamID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public UserPrivilege Privilege { get; set; }
    }

    [Flags]
    public enum UserPrivilege : ulong
    {
        None = 0UL,
        BanPlayer = 1UL << 0,


        Admin = ulong.MaxValue
    }
}
