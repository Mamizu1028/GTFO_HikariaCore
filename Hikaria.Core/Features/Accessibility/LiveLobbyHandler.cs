using Clonesoft.Json;
using Hikaria.Core.Entities;
using Hikaria.Core.Features.Security;
using Hikaria.Core.Interfaces;
using Hikaria.Core.Utility;
using SNetwork;
using System.Globalization;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Managers;

namespace Hikaria.Core.Features.Accessibility
{
    [AutomatedFeature]
    [DoNotSaveToConfig]
    public class LiveLobbyHandler : Feature, IOnSessionMemberChanged, IOnMasterChanged
    {
        public override string Name => "Live Lobby Automator";

        public override FeatureGroup Group => EntryPoint.Groups.Accessibility;

        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (!player.IsLocal || !SNet.IsMaster)
            {
                return;
            }
            switch (playerEvent)
            {
                case SessionMemberEvent.JoinSessionHub:
                    if (player.IsLocal && SNet.IsMaster)
                    {
                    }
                    break;
                case SessionMemberEvent.LeftSessionHub:
                    if (player.IsLocal && SNet.IsMaster)
                    {

                    }
                    break;
            }
        }


        public void OnMasterChanged()
        {
            if (SNet.IsMaster && SNet.IsInLobby)
            {
                CreateLobby();
            }
        }

        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
        }

        public static LiveLobby CurrentLiveLobby { get; private set; }

        [ArchivePatch(typeof(SNet_Lobby_STEAM), nameof(SNet_Lobby_STEAM.OnLocalPlayerJoinedLobby))]
        private class SNet_Lobby_STEAM__OnLocalPlayerJoinedLobby__Patch
        {
            private static void Postfix()
            {
                CreateLobby();
            }
        }

        [ArchivePatch(typeof(SNet_Lobby_STEAM), nameof(SNet_Lobby_STEAM.KeepLobbyAliveAndConnected))]
        private class SNet_Lobby_STEAM__KeepLobbyAliveAndConnected__Patch
        {
            private static void Postfix(SNet_Lobby_STEAM __instance)
            {
                KeepLobbyAlive(__instance);
            }
        }

        [ArchivePatch(typeof(SNet_Lobby_STEAM), nameof(SNet_Lobby_STEAM.OnLobbyUpdate))]
        private class SNet_Lobby_STEAM__OnLobbyUpdate__Patch
        {
            private static void Postfix(SNet_Lobby_STEAM __instance)
            {
                UpdateLobbyDetailInfo(__instance);
            }
        }


        private static void CreateLobby()
        {
            if (!SNet.IsMaster)
                return;
            var settings = LobbySettingsOverride.LobbySettingsManager.CurrentSettings;
            LobbyIdentifier identifier = new()
            {
                ID = SNet.Lobby.Identifier.ID,
                Name = SNet.Lobby.Identifier.Name,
            };
            LobbySettings setting = new()
            {
                LobbyName = identifier.Name,
                LobbyType = (Entities.LobbyType)(int)LobbySettingsOverride.Settings.Privacy,
                Password = settings.Password,
            };
            DetailedLobbyInfo detailedInfo = new()
            {
                Rundown = PresenceManager.Rundown,
                Expedition = PresenceManager.Expedition,
                ExpeditionName = PresenceManager.ExpeditionName,
                HostSteamID = SNet.LocalPlayer.Lookup,
                LobbyName = identifier.Name,
                MaxPlayerSlots = PresenceManager.MaxPlayerSlots,
                OpenSlots = PresenceManager.OpenSlots,
                RegionName = RegionInfo.CurrentRegion.TwoLetterISORegionName,
                Revision = SNet.GameRevision,
                StatusInfo = string.Empty
            };

            CurrentLiveLobby = new(identifier, setting, detailedInfo);

            HttpClientHelper httpClient = new();
            httpClient.PostAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/CreateLobby", CurrentLiveLobby);

            Logs.LogMessage($"PostCreateLobby: {JsonConvert.SerializeObject(CurrentLiveLobby, Formatting.Indented)}");
        }

        private static void KeepLobbyAlive(SNet_Lobby_STEAM lobby)
        {
            if (!SNet.IsMaster)
                return;
            HttpClientHelper httpClient = new();
            httpClient.PostAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/KeepLobbyAlive?revision={SNet.GameRevision}&lobbyID={lobby.Identifier.ID}", string.Empty);
            Logs.LogMessage($"PostKeepLobbyAlive: Revision={SNet.GameRevision}, LobbyID={lobby.Identifier.ID}");
        }

        private static void UpdateLobbyDetailInfo(SNet_Lobby_STEAM lobby)
        {
            if (!SNet.IsMaster)
                return;
            DetailedLobbyInfo detailedInfo = new()
            {
                Rundown = PresenceManager.Rundown,
                Expedition = PresenceManager.Expedition,
                ExpeditionName = PresenceManager.ExpeditionName,
                HostSteamID = SNet.LocalPlayer.Lookup,
                LobbyName = lobby.Identifier.Name,
                MaxPlayerSlots = PresenceManager.MaxPlayerSlots,
                OpenSlots = PresenceManager.OpenSlots,
                RegionName = RegionInfo.CurrentRegion.TwoLetterISORegionName,
                Revision = SNet.GameRevision,
                StatusInfo = string.Empty
            };

            CurrentLiveLobby.UpdateInfo(detailedInfo);
            HttpClientHelper httpClient = new();
            httpClient.PostAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/UpdateLobbyInfo?revision={SNet.GameRevision}&lobbyID={lobby.Identifier.ID}", detailedInfo);
            Logs.LogMessage($"PostKeepLobbyAlive: Revision={SNet.GameRevision}, LobbyID={lobby.Identifier.ID}");
        }
    }
}
