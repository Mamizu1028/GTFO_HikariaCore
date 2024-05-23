using Hikaria.Core.Entities;
using Hikaria.Core.Features.Security;
using SNetwork;
using System.Globalization;
using TheArchive;
using TheArchive.Core.Managers;
using TheArchive.Features.Presence;
using TheArchive.Utilities;

namespace Hikaria.Core.Managers
{
    public static class LiveLobbyPresenceManager
    {
        public static int Revision => SNet.GameRevision;
        public static bool IsPlayingModded => ArchiveMod.IsPlayingModded;
        public static string Expedition => PresenceManager.Expedition;
        public static string ExpeditionName => RichPresenceCore.ExpeditionName;
        public static int MaxPlayerSlots => SNet.Slots.m_playerSlotPermissions.Where((_, i) => SNet.Slots.IsHumanPermittedInSlot(i)).Count();
        public static int OpenSlots => SNet.Slots.m_playerSlotPermissions.Where((_, i) => SNet.Slots.IsHumanPermittedInSlot(i) && (SNet.Slots.PlayerSlots[i].player == null || SNet.Slots.PlayerSlots[i].player.IsBot)).Count();
        public static bool IsLobbyFull => !SNet.Slots.HasFreeHumanSlot();

        public static LobbyIdentifier Identifier => new()
        {
            ID = SNet.Lobby?.Identifier?.ID ?? 0UL,
            Name = SNet.Lobby?.Identifier?.Name ?? string.Empty,
        };
        public static LobbyPrivacySettings PrivacySettings => new()
        {
            Privacy = LobbySettingsOverride.LobbySettingsManager.CurrentSettings.Privacy,
            HasPassword = LobbySettingsOverride.LobbySettingsManager.CurrentSettings.HasPassword,
        };
        public static DetailedLobbyInfo DetailedInfo => new()
        {
            Expedition = Expedition,
            ExpeditionName = ExpeditionName,
            HostSteamID = SNet.Master?.Lookup ?? 0UL,
            IsPlayingModded = IsPlayingModded,
            MaxPlayerSlots = MaxPlayerSlots,
            OpenSlots = OpenSlots,
            RegionName = RegionInfo.CurrentRegion.TwoLetterISORegionName,
            Revision = Revision,
            SteamIDsInLobby = SNet.Lobby?.TryCast<SNet_Lobby_STEAM>()?.m_steamIDsInLobby.ToSystemList().ToHashSet() ?? new(),
        };
        public static LobbyStatusInfo StatusInfo => new();

        public static LobbyPrivacy GetLobbyTypeFromSNetLobbyType(LobbyType type)
        {
            return type switch
            {
                LobbyType.Invisible => LobbyPrivacy.Invisible,
                LobbyType.Private => LobbyPrivacy.Private,
                LobbyType.Public => LobbyPrivacy.Public,
                LobbyType.FriendsOnly => LobbyPrivacy.FriendsOnly,
                _ => LobbyPrivacy.Invisible
            };
        }
    }
}
