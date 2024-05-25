namespace Hikaria.Core.Entities
{
    public class LiveLobby
    {
        public ulong LobbyID { get; set; }
        public string LobbyName { get; set; }
        public LobbyPrivacySettings PrivacySettings { get; set; }
        public DetailedLobbyInfo DetailedInfo { get; set; }
        public LobbyStatusInfo StatusInfo { get; set; }

        public DateTime ExpirationTime { get; set; }

        public LiveLobby()
        {
            LobbyName = string.Empty;
            PrivacySettings = new();
            DetailedInfo = new();
            StatusInfo = new();
            ExpirationTime = DateTime.Now.AddSeconds(30);
        }

        public LiveLobby(ulong lobbyID, string lobbyName, LobbyPrivacySettings lobbyPrivacySettings, DetailedLobbyInfo detailedInfo) : this()
        {
            LobbyID = lobbyID;
            LobbyName = lobbyName;
            PrivacySettings = lobbyPrivacySettings;
            DetailedInfo = detailedInfo;
        }
    }


    public class LobbyPrivacySettings
    {
        public LobbyPrivacy Privacy
        {
            get
            {
                if (HasPassword && _privacy == LobbyPrivacy.Private)
                    return LobbyPrivacy.Private;
                return _privacy;
            }
            set
            {
                _privacy = value;
            }
        }

        public bool HasPassword { get; set; }

        private LobbyPrivacy _privacy = LobbyPrivacy.Public;
    }

    public class DetailedLobbyInfo
    {
        public ulong HostSteamID { get; set; }
        public string Expedition { get; set; }
        public string ExpeditionName { get; set; }
        public int OpenSlots { get; set; }
        public int MaxPlayerSlots { get; set; }
        public string RegionName { get; set; }
        public int Revision { get; set; }
        public bool IsPlayingModded { get; set; }
        public HashSet<ulong> SteamIDsInLobby { get; set; }

        public DetailedLobbyInfo()
        {
            Expedition = string.Empty;
            ExpeditionName = string.Empty;
            RegionName = string.Empty;
            SteamIDsInLobby = new();
        }
    }

    public class LobbyStatusInfo
    {
        public string StatusInfo { get; set; }

        public LobbyStatusInfo()
        {
            StatusInfo = string.Empty;
        }
    }

    public class LiveLobbyQueryBase
    {
        public virtual bool IgnoreFullLobby { get; set; } = true;
        public virtual bool IsPlayingModded { get; set; } = false;
        public virtual string LobbyName { get; set; } = string.Empty;
        public virtual LobbyPrivacy Privacy { get; set; } = LobbyPrivacy.Public;
        public virtual string Expedition { get; set; } = string.Empty;
        public virtual string ExpeditionName { get; set; } = string.Empty;
        public virtual string RegionName { get; set; } = string.Empty;
        public virtual int Revision { get; set; } = -1;
    }

    public enum LobbyPrivacy
    {
        Public = 0,
        FriendsOnly = 1,
        Private = 2,
        Invisible = 3
    }
}
