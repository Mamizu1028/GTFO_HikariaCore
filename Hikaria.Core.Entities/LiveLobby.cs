using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hikaria.Core.Entities
{
    public class LiveLobby
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong LobbyID { get; set; }
        [Required]
        [MaxLength(100)]
        public string LobbyName { get; set; }
        [Required]
        public LobbyPrivacySettings PrivacySettings { get; set; }
        [Required]
        public DetailedLobbyInfo DetailedInfo { get; set; }
        [Required]
        public LobbyStatusInfo StatusInfo { get; set; }
        [Required]
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
        [Required]
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
        [Required]
        public bool HasPassword { get; set; }

        private LobbyPrivacy _privacy = LobbyPrivacy.Public;
    }

    public class DetailedLobbyInfo
    {
        [Required]
        public ulong HostSteamID { get; set; }
        [Required]
        [MaxLength(100)]
        public string Expedition { get; set; }
        [Required]
        [MaxLength(100)]
        public string ExpeditionName { get; set; }
        [Required]
        public int OpenSlots { get; set; }
        [Required]
        public int MaxPlayerSlots { get; set; }
        [Required]
        [MaxLength(100)]
        public string RegionName { get; set; }
        [Required]
        public int Revision { get; set; }
        [Required]
        public bool IsPlayingModded { get; set; }
        [Required]
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
        [Required]
        [MaxLength(500)]
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
