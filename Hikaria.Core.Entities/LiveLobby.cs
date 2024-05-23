namespace Hikaria.Core.Entities
{
    public class LiveLobby
    {
        public LobbyIdentifier Identifier { get; private set; } = new();
        public LobbyPrivacySettings PrivacySettings { get; private set; } = new();
        public DetailedLobbyInfo DetailedInfo { get; private set; } = new();
        public LobbyStatusInfo StatusInfo { get; private set; } = new();

        public DateTime ExpirationTime { get; private set; }

        public LiveLobby()
        {
            ExpirationTime = DateTime.Now.AddSeconds(30);
        }

        public LiveLobby(LobbyIdentifier identifier, LobbyPrivacySettings lobbyPrivacySettings, DetailedLobbyInfo detailedInfo)
        {
            PrivacySettings = lobbyPrivacySettings;
            Identifier = identifier;
            DetailedInfo = detailedInfo;
            ExpirationTime = DateTime.Now.AddSeconds(30);
        }

        public void KeepAlive()
        {
            ExpirationTime = DateTime.Now.AddSeconds(30);
        }

        public void UpdateInfo(DetailedLobbyInfo detailedInfo)
        {
            DetailedInfo = detailedInfo;
        }

        public void UpdatePrivacySettings(LobbyPrivacySettings settings)
        {
            PrivacySettings = settings;
        }

        public void UpdateStatusInfo(LobbyStatusInfo statusInfo)
        {
            StatusInfo = statusInfo;
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
        public string Expedition { get; set; } = string.Empty;
        public string ExpeditionName { get; set; } = string.Empty;
        public int OpenSlots { get; set; }
        public int MaxPlayerSlots { get; set; }
        public string RegionName { get; set; } = string.Empty;
        public int Revision { get; set; }
        public bool IsPlayingModded { get; set; }
        public HashSet<ulong> SteamIDsInLobby { get; set; } = new();
    }

    public class LobbyStatusInfo
    {
        public string StatusInfo { get; set; } = string.Empty;
    }

    public class LobbyIdentifier
    {
        public ulong ID { get; set; }
        public string Name { get; set; }
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
