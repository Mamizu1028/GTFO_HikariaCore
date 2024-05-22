namespace Hikaria.Core.Entities
{
    public class LiveLobby
    {
        public LobbyIdentifier Identifier { get; private set; } = new();
        public LobbySettings Settings { get; private set; } = new();
        public DetailedLobbyInfo DetailedInfo { get; private set; } = new();

        public DateTime ExpirationTime { get; private set; }
        public bool IsLobbyFull => SteamIDsInLobby.Count >= DetailedInfo.MaxPlayerSlots;
        public HashSet<ulong> SteamIDsInLobby { get; private set; } = new();

        public LiveLobby(LobbyIdentifier identifier, LobbySettings settings, DetailedLobbyInfo detailedInfo)
        {
            Settings = settings;
            Identifier = identifier;
            DetailedInfo = detailedInfo;
            ExpirationTime = DateTime.Now.AddSeconds(15);
        }

        public void KeepAlive(bool sync = false)
        {
            ExpirationTime = DateTime.Now.AddSeconds(15);
        }

        public void UpdateInfo(DetailedLobbyInfo detailedInfo)
        {
            DetailedInfo = detailedInfo;
        }

        public void UpdateSettings(LobbySettings settings)
        {
            Settings = settings;
        }

        public void UpdateStateInfo(string statusInfo)
        {
            DetailedInfo.StatusInfo = statusInfo;
        }

        public void PlayerJoined(ulong steamid)
        {
            SteamIDsInLobby.Add(steamid);
        }

        public void PlayerLeft(ulong steamid)
        {
            SteamIDsInLobby.Remove(steamid);
        }

        public void OnLobbyUpdate()
        {

        }
    }


    public class LobbySettings
    {
        public string LobbyName { get; set; }
        public LobbyType LobbyType
        {
            get
            {
                if (UsePassword)
                    return LobbyType.Private;
                return _lobbyType;
            }
            set
            {
                _lobbyType = value;
            }
        }

        public bool UsePassword => _lobbyType == LobbyType.Private && !string.IsNullOrEmpty(_password);
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                value ??= string.Empty;
                value = value[..Math.Min(value.Length, PASSWORD_MAX_LENGTH)];
                _password = value;
            }
        }

        private LobbyType _lobbyType = LobbyType.Public;
        private string _password = string.Empty;
        public const int PASSWORD_MAX_LENGTH = 25;
    }

    public class DetailedLobbyInfo
    {
        public ulong HostSteamID { get; set; }
        public string LobbyName { get; set; }
        public string Rundown { get; set; }
        public string Expedition { get; set; }
        public string ExpeditionName { get; set; }
        public int OpenSlots { get; set; }
        public int MaxPlayerSlots { get; set; }
        public string StatusInfo { get; set; }
        public string RegionName { get; set; }
        public int Revision { get; set; }
    }

    public class LobbyIdentifier
    {
        public ulong ID { get; set; }
        public string Name { get; set; }
    }

    public enum LobbyType
    {
        Public = 0,
        FriendsOnly = 1,
        Private = 2,
        Invisible = 3
    }
}
