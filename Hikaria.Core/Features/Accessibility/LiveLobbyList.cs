using Clonesoft.Json;
using Hikaria.Core.Entities;
using Hikaria.Core.Interfaces;
using Hikaria.Core.Managers;
using Hikaria.Core.Utility;
using SNetwork;
using Steamworks;
using System.Globalization;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TMPro;
using static Hikaria.Core.Features.Security.LobbySettingsOverride;
using LobbyPrivacy = Hikaria.Core.Entities.LobbyPrivacy;

namespace Hikaria.Core.Features.Accessibility
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    internal class LiveLobbyList : Feature, IOnMasterChanged, IOnPlayerSlotChanged
    {
        public override string Name => "在线大厅列表";

        public override FeatureGroup Group => EntryPoint.Groups.Accessibility;

        public override Type[] LocalizationExternalTypes => new[]
        {
            typeof(LobbyPrivacy)
        };

        [FeatureConfig]
        public static LiveLobbyListSetting Settings { get; set; }

        public class LiveLobbyListSetting
        {
            [FSInline]
            [JsonIgnore]
            public LiveLobbyQueryEntry LobbyQueryEntry { get; set; } = new();
            [JsonIgnore]
            [FSHeader("大厅列表")]
            [FSDisplayName("大厅列表")]
            public List<LiveLobbyEntry> LiveLobbyEntries { get; set; } = new();
        }

        public class LiveLobbyQueryEntry : LiveLobbyQueryBase
        {
            public LiveLobbyQueryEntry()
            {
                QueryButton = new("搜索", "搜索", () =>
                {
                    Task.Run(async () =>
                    {
                        PromptLabel.PrimaryText.Cast<TextMeshPro>().SetText($"正在搜索...");
                        Settings.LiveLobbyEntries = new();
                        var result = await QueryLiveLobby(this);
                        foreach (var lobby in result)
                        {
                            Settings.LiveLobbyEntries.Add(new(lobby));
                        }
                        PromptLabel.PrimaryText.Cast<TextMeshPro>().SetText($"已搜索到{Settings.LiveLobbyEntries.Count}个符合条件的大厅");
                    });
                });
            }

            [FSHeader("大厅搜索")]
            [FSDisplayName("关卡")]
            public override string Expedition { get; set; } = string.Empty;
            [FSDisplayName("关卡名称")]
            public override string ExpeditionName { get; set; } = string.Empty;
            [FSDisplayName("大厅名称")]
            public override string LobbyName { get; set; } = string.Empty;
            [FSDisplayName("国家/地区")]
            [FSDescription("在 ISO 3166 中定义的由两个字母组成的国家/地区代码，如 \"CN\"")]
            public override string RegionName { get; set; } = string.Empty;
            [FSDisplayName("MTFO标记")]
            public override bool IsPlayingModded { get; set; } = false;
            [FSDisplayName("大厅权限")]
            public override LobbyPrivacy Privacy { get; set; } = LobbyPrivacy.Public;
            [FSDisplayName("忽略已满大厅")]
            public override bool IgnoreFullLobby { get; set; } = true;

            [FSIgnore]
            public override int Revision => CoreGlobal.Revision;
            [JsonIgnore]
            public FLabel EmptyLabel1 { get; set; } = new();
            [JsonIgnore]
            public FLabel PromptLabel { get; set; } = new("点按下方按钮搜索符合条件的大厅");
            [JsonIgnore]
            [FSDisplayName("搜索大厅")]
            public FButton QueryButton { get; set; }
            [JsonIgnore]
            public FLabel EmptyLabel2 { get; set; } = new();
        }

        public class LiveLobbyEntry
        {
            public LiveLobbyEntry(LiveLobby lobby)
            {
                LobbyID = lobby.LobbyID;
                LobbyName = lobby.LobbyName;
                Privacy = lobby.PrivacySettings.Privacy;
                ExpeditionInfo = $"{lobby.DetailedInfo.Expedition} \"{lobby.DetailedInfo.ExpeditionName}\"";
                SecondaryEntry.RegionName = new RegionInfo(lobby.DetailedInfo.RegionName).DisplayName;
                SecondaryEntry.StatusInfo = lobby.StatusInfo?.StatusInfo ?? string.Empty;
                SlotsInfo = $"{lobby.DetailedInfo.MaxPlayerSlots - lobby.DetailedInfo.OpenSlots}/{lobby.DetailedInfo.MaxPlayerSlots}";
                SecondaryEntry.IsPlayeringModded = lobby.DetailedInfo.IsPlayingModded;
                JoinButton = new("加入", "加入", () =>
                {
                    if (Privacy == LobbyPrivacy.Private)
                    {
                        LobbySettingsManager.PasswordForJoinOtherLobby = Password;
                    }
                    SNet.Lobbies.JoinLobby(LobbyID);
                });
            }

            [FSSeparator]
            [FSDisplayName("大厅ID")]
            [FSReadOnly]
            public ulong LobbyID { get; set; }
            [FSDisplayName("大厅名称")]
            [FSReadOnly]
            public string LobbyName { get; set; }
            [FSDisplayName("关卡")]
            [FSReadOnly]
            public string ExpeditionInfo { get; set; }
            [FSDisplayName("人数")]
            [FSReadOnly]
            public string SlotsInfo { get; set; }
            [FSDisplayName("权限")]
            [FSReadOnly]
            public LobbyPrivacy Privacy { get; set; }
            [FSDisplayName("大厅详情")]
            public LiveLobbySecondaryEntry SecondaryEntry { get; set; } = new();
            [FSHeader("加入大厅")]
            [FSDescription("若为大厅权限为私密则必须输入正确密码才可加入")]
            [FSDisplayName("加入密码")]
            [FSMaxLength(25)]
            public string Password { get; set; }
            [FSDisplayName("加入大厅")]
            public FButton JoinButton { get; set; }
        }

        public class LiveLobbySecondaryEntry
        {
            [FSDisplayName("MTFO标记")]
            [FSReadOnly]
            public bool IsPlayeringModded { get; set; }
            [FSDisplayName("国家/地区")]
            [FSReadOnly]
            public string RegionName { get; set; }
            [FSDisplayName("状态")]
            [FSReadOnly]
            public string StatusInfo { get; set; }
        }

        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
        }

        public void OnMasterChanged()
        {
            if (SNet.IsMaster && SNet.IsInLobby)
            {
                Task.Run(async () =>
                {
                    await CreateLobby();
                    await UpdateLobbyDetailInfo(SNet.Lobby.TryCast<SNet_Lobby_STEAM>());
                    await UpdateLobbyPrivacySettings(SNet.Lobby.TryCast<SNet_Lobby_STEAM>());
                });

            }
        }

        public void OnPlayerSlotChanged(SNet_Player player, SNet_SlotType type, SNet_SlotHandleType handle, int index)
        {
            if (SNet.IsMaster && SNet.IsInLobby)
            {
                UpdateLobbyDetailInfo(SNet.Lobby.TryCast<SNet_Lobby_STEAM>());
            }
        }

        [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.OnLobbyInfoUpdate))]
        private class SNet_SessionHub__OnLobbyInfoUpdate__Patch
        {
            private static void Postfix()
            {
                if (SNet.IsMaster && SNet.IsInLobby)
                {
                    CreateLobby();
                }
            }
        }

        [ArchivePatch(typeof(SNet_PlayerSlotManager), nameof(SNet_PlayerSlotManager.SetSlotPermission))]
        private class SNet_PlayerSlotManager__SetSlotPermission__Patch
        {
            private static void Postfix()
            {
                if (SNet.IsMaster && SNet.IsInLobby)
                {
                    UpdateLobbyDetailInfo(SNet.Lobby.TryCast<SNet_Lobby_STEAM>());
                }
            }
        }

        [ArchivePatch(typeof(SNet_PlayerSlotManager), nameof(SNet_PlayerSlotManager.SetAllPlayerSlotPermissions))]
        private class SNet_PlayerSlotManager__SetAllPlayerSlotPermissions__Patch
        {
            private static void Postfix()
            {
                if (SNet.IsMaster && SNet.IsInLobby)
                {
                    UpdateLobbyDetailInfo(SNet.Lobby.TryCast<SNet_Lobby_STEAM>());
                }
            }
        }

        [ArchivePatch(typeof(RundownManager), nameof(RundownManager.SetActiveExpedition))]
        private class RundownManager__SetActiveExpedition__Patch
        {
            private static void Postfix()
            {
                if (SNet.IsMaster && SNet.IsInLobby)
                {
                    UpdateLobbyDetailInfo(SNet.Lobby.TryCast<SNet_Lobby_STEAM>());
                }
            }
        }

        [ArchivePatch(typeof(SNet_Lobby_STEAM), nameof(SNet_Lobby_STEAM.KeepLobbyAliveAndConnected))]
        private class SNet_Lobby_STEAM__KeepLobbyAliveAndConnected__Patch
        {
            private static void Postfix(SNet_Lobby_STEAM __instance)
            {
                if (SNet.IsMaster && SNet.IsInLobby)
                {
                    KeepLobbyAlive(__instance);
                }
            }
        }

        [ArchivePatch(typeof(SNet_Lobby_STEAM), nameof(SNet_Lobby_STEAM.PlayerJoined), new Type[] { typeof(SNet_Player), typeof(CSteamID) })]
        private class SNet_Lobby_STEAM__PlayerJoined__Patch
        {
            private static void Postfix(SNet_Lobby_STEAM __instance, SNet_Player player)
            {
                if (SNet.IsMaster && SNet.IsInLobby)
                {
                    if (player.IsLocal)
                        CreateLobby();
                    else
                        UpdateLobbyDetailInfo(__instance);
                }
            }
        }

        [ArchivePatch(typeof(SNet_Lobby_STEAM), nameof(SNet_Lobby_STEAM.PlayerLeft))]
        private class SNet_Lobby_STEAM__PlayerLeft__Patch
        {
            private static void Postfix(SNet_Lobby_STEAM __instance, SNet_Player player)
            {
                if (SNet.IsMaster && SNet.IsInLobby)
                {
                    if (!player.IsLocal)
                    {
                        UpdateLobbyDetailInfo(__instance);
                    }
                }
            }
        }

        public static async Task<IEnumerable<LiveLobby>> QueryLiveLobby(LiveLobbyQueryBase filter)
        {
            return await HttpHelper.PostAsync<List<LiveLobby>>($"{CoreGlobal.ServerUrl}/LiveLobby/QueryLobby", filter);
        }

        public static async Task CreateLobby()
        {
            await HttpHelper.PutAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/CreateLobby", new LiveLobby(LiveLobbyPresenceManager.LobbyID, LiveLobbyPresenceManager.LobbyName, LiveLobbyPresenceManager.PrivacySettings, LiveLobbyPresenceManager.DetailedInfo));
        }

        public static async Task KeepLobbyAlive(SNet_Lobby_STEAM lobby)
        {
            await HttpHelper.PatchAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/KeepLobbyAlive?revision={SNet.GameRevision}&lobbyID={lobby.Identifier.ID}", string.Empty);
        }

        public static async Task UpdateLobbyDetailInfo(SNet_Lobby_STEAM lobby)
        {
            await HttpHelper.PatchAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/UpdateLobbyDetailedInfo?revision={SNet.GameRevision}&lobbyID={lobby.Identifier.ID}", LiveLobbyPresenceManager.DetailedInfo);
        }

        public static async Task UpdateLobbyPrivacySettings(SNet_Lobby_STEAM lobby)
        {
            await HttpHelper.PatchAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/UpdateLobbyPrivacySettings?revision={SNet.GameRevision}&lobbyID={lobby.Identifier.ID}", LiveLobbyPresenceManager.PrivacySettings);
        }

        public static async Task UpdateLobbyStatusInfo(SNet_Lobby_STEAM lobby)
        {
            await HttpHelper.PatchAsync<object>($"{CoreGlobal.ServerUrl}/LiveLobby/UpdateLobbyStatusInfo?revision={SNet.GameRevision}&lobbyID={lobby.Identifier.ID}", LiveLobbyPresenceManager.StatusInfo);
        }
    }
}
