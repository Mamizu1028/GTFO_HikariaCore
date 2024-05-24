using Clonesoft.Json;
using Hikaria.Core.Entities;
using Hikaria.Core.Features.Accessibility;
using Hikaria.Core.Interfaces;
using Hikaria.Core.Managers;
using Hikaria.Core.SNetworkExt;
using SNetwork;
using Steamworks;
using System.Runtime.InteropServices;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Core.Localization;
using TheArchive.Features.Security;
using TheArchive.Interfaces;
using TheArchive.Loader;
using TheArchive.Utilities;

namespace Hikaria.Core.Features.Security;

[DisallowInGameToggle]
[EnableFeatureByDefault]
internal class LobbySettingsOverride : Feature, IOnSessionMemberChanged
{
    public override string Name => "大厅设置覆盖";

    public override string Description => "提供大厅权限和密码的设置。";

    public override FeatureGroup Group => EntryPoint.Groups.Security;

    public static new IArchiveLogger FeatureLogger { get; set; }

    public static ILocalizationService LocalizationService { get; private set; }

    public override Type[] LocalizationExternalTypes => new[]
    {
        typeof(LobbyPrivacy)
    };

    [FeatureConfig]
    public static LobbySettingOverrideSettings Settings { get; set; }

    public class LobbySettingOverrideSettings
    {
        [JsonIgnore]
        [FSDisplayName("Privacy")]
        public LobbyPrivacy Privacy
        {
            get
            {
                return LobbySettingsManager.CurrentSettings.Privacy;
            }
            set
            {
                LobbySettingsManager.CurrentSettings.Privacy = value;
                LobbySettingsManager.OnLobbySettingsChanged();
            }
        }
        [JsonIgnore]
        [FSMaxLength(25), FSDisplayName("Password")]
        public string Password
        {
            get
            {
                return LobbySettingsManager.CurrentSettings.Password;
            }
            set
            {
                LobbySettingsManager.CurrentSettings.Password = value;
                LobbySettingsManager.OnLobbySettingsChanged();
            }
        }
        [JsonIgnore]
        [FSHeader("Join Other Lobby")]
        [FSMaxLength(25), FSDisplayName("Password For Join Other Lobby")]
        public string PasswordForJoinOthers
        {
            get
            {
                return LobbySettingsManager.PasswordForJoinOtherLobby;
            }
            set
            {
                LobbySettingsManager.PasswordForJoinOtherLobby = value;
            }
        }
    }

    public override void OnFeatureSettingChanged(FeatureSetting setting)
    {
        if (SNet.IsMaster && SNet.IsInLobby)
        {
            LiveLobbyList.UpdateLobbyPrivacySettings(SNet.Lobby.TryCast<SNet_Lobby_STEAM>());
        }
    }


    [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.SlaveSendSessionQuestion))]
    private class SNet_SessionHub__SlaveSendSessionQuestion__Patch
    {
        private static void Prefix(SlaveSessionQuestion question)
        {
            LobbySettingsManager.SlaveSendSessionRequest(question);
        }
    }

    [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.OnSlaveQuestion))]
    private class SNet_SessionHub__OnSlaveQuestion__Patch
    {
        private static bool Prefix(pSlaveQuestion data)
        {
            return LobbySettingsManager.OnSlaveQuestionOverride(data);
        }
    }

    [ArchivePatch(typeof(SNet_LobbyManager), nameof(SNet_LobbyManager.CreateLobby))]
    private class SNet_LobbyManager__CreateLobby__Patch
    {
        private static void Prefix(ref SNet_LobbySettings settings)
        {
            if (settings != null)
            {
                LobbySettingsManager.ApplyLobbySettings(ref settings);
            }
        }
    }

    [ArchivePatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.InviteUserToLobby))]
    private class SteamMatchmaking__InviteUserToLobby__Patch
    {
        private static void Postfix(CSteamID steamIDInvitee)
        {
            LobbySettingsManager.WhitelistPlayer(steamIDInvitee.m_SteamID);
        }
    }

    public override void Init()
    {
        LocalizationService = Localization;
        LobbySettingsManager.Setup();
    }

    public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            LobbySettingsManager.DoPlayerLeftCleanup(player);
        }
    }

    public static class LobbySettingsManager
    {
        private static IArchiveLogger _logger;
        private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateLoggerInstance(nameof(LobbySettingsManager));

        private static SNetExt_Packet<pSlaveRequest> s_slaveSessionRequestPacket;
        private static SNetExt_Packet<pLobbyMasterAnswer> s_lobbySettingsAnswerPacket;

        private static Dictionary<ulong, pSlaveRequest> s_receivedSlaveRequestsLookup = new();
        private static HashSet<ulong> s_tempWhitelist = new();
        public static LobbySettings CurrentSettings { get; private set; } = new();

        public static SNet_Lobby_STEAM SteamLobby => SNet.Lobby?.TryCast<SNet_Lobby_STEAM>();
        public static SNet_Core_STEAM Core => SNet.Core.TryCast<SNet_Core_STEAM>();

        public static string PasswordForJoinOtherLobby
        {
            get
            {
                return _passwordForJoinOthers;
            }
            set
            {
                value ??= string.Empty;
                value = value[..Math.Min(value.Length, 25)];
                _passwordForJoinOthers = value;
            }
        }
        private static string _passwordForJoinOthers = string.Empty;

        public static void Setup()
        {
            s_slaveSessionRequestPacket = SNetExt_Packet<pSlaveRequest>.Create(typeof(pSlaveRequest).FullName, OnReceiveSlaveRequest, null, false, SNet_ChannelType.SessionOrderCritical);
            s_lobbySettingsAnswerPacket = SNetExt_Packet<pLobbyMasterAnswer>.Create(typeof(pLobbyMasterAnswer).FullName, OnReceiveLobbySettingsAnswer, null, false, SNet_ChannelType.SessionOrderCritical);
        }

        public static void ApplyLobbySettings(ref SNet_LobbySettings settings)
        {
            settings.Password = CurrentSettings.Password;
            settings.LobbyType = ToSNetLobbyType(CurrentSettings.Privacy);
            settings.LobbyName = CurrentSettings.LobbyName;
        }

        public static void OnLobbySettingsChanged()
        {
            if (!SNet.IsInLobby || !SNet.IsMaster || SteamLobby == null) return;

            var settings = Core.m_lastLobbySettings;
            settings.Password = CurrentSettings.Password;
            settings.LobbyType = ToSNetLobbyType(CurrentSettings.Privacy);
            settings.LobbyName = CurrentSettings.LobbyName;

            SteamLobby.Password = CurrentSettings.Password;
            SteamLobby.Name = CurrentSettings.LobbyName;
            SteamLobby.Identifier.Name = CurrentSettings.LobbyName;
        }

        private static void OnReceiveSlaveRequest(ulong sender, pSlaveRequest data)
        {
            if (!SNet.IsMaster) return;

            s_receivedSlaveRequestsLookup[sender] = data;
        }

        private static void OnReceiveLobbySettingsAnswer(ulong sender, pLobbyMasterAnswer data)
        {
            if (!SNet.Replication.IsLastSenderMaster()) return;

            if (data.Answer != MasterAnswer.LeaveLobby)
            {
                return;
            }

            if (data.Reason == MasterAnswerReason.Banned)
            {
                PopupMessageManager.ShowPopup(Popup_Banned);
                return;
            }

            switch (data.LobbyPrivacy)
            {
                case LobbyPrivacy.Invisible:
                    PopupMessageManager.ShowPopup(Popup_InvisibleLobby);
                    break;
                case LobbyPrivacy.Private:
                    switch (data.Reason)
                    {
                        case MasterAnswerReason.PasswordMismatch:
                            PopupMessageManager.ShowPopup(Popup_PasswordMismatch);
                            break;
                    }
                    break;
                case LobbyPrivacy.FriendsOnly:
                    switch (data.Reason)
                    {
                        case MasterAnswerReason.IsNotFriend:
                            PopupMessageManager.ShowPopup(Popup_IsNotFriend);
                            break;
                    }
                    break;
                default:
                    return;
            }
        }

        private static PopupMessage Popup_Banned => new()
        {
            BlinkInContent = true,
            BlinkTimeInterval = 0.5f,
            Header = LocalizationService.Get(1),
            UpperText = "<color=red>无法加入大厅</color>\n\n原因：被封禁的玩家",
            LowerText = string.Empty,
            PopupType = PopupType.BoosterImplantMissed,
            OnCloseCallback = new Action(() =>
            {
                if (SNet.LocalPlayer.IsOutOfSync)
                {
                    SNet.SessionHub.LeaveHub();
                }
            })
        };

        private static PopupMessage Popup_InvisibleLobby => new()
        {
            BlinkInContent = true,
            BlinkTimeInterval = 0.5f,
            Header = LocalizationService.Get(1),
            UpperText = "<color=red>无法加入大厅</color>\n\n原因：大厅已锁定",
            LowerText = string.Empty,
            PopupType = PopupType.BoosterImplantMissed,
            OnCloseCallback = new Action(() =>
            {
                if (SNet.LocalPlayer.IsOutOfSync)
                {
                    SNet.SessionHub.LeaveHub();
                }
            })
        };

        private static PopupMessage Popup_PasswordMismatch => new()
        {
            BlinkInContent = true,
            BlinkTimeInterval = 0.5f,
            Header = LocalizationService.Get(1),
            UpperText = "<color=red>无法加入大厅</color>\n\n原因：密码错误",
            LowerText = string.Empty,
            PopupType = PopupType.BoosterImplantMissed,
            OnCloseCallback = new Action(() =>
            {
                if (SNet.LocalPlayer.IsOutOfSync)
                {
                    SNet.SessionHub.LeaveHub();
                }
            })
        };

        private static PopupMessage Popup_IsNotFriend => new()
        {
            BlinkInContent = true,
            BlinkTimeInterval = 0.5f,
            Header = LocalizationService.Get(1),
            UpperText = "<color=red>无法加入大厅</color>\n\n原因：您不是房主好友",
            LowerText = string.Empty,
            PopupType = PopupType.BoosterImplantMissed,
            OnCloseCallback = new Action(() =>
            {
                if (SNet.LocalPlayer.IsOutOfSync)
                {
                    SNet.SessionHub.LeaveHub();
                }
            })
        };

        public static void SlaveSendSessionRequest(SlaveSessionQuestion question)
        {
            if (!SNet.HasMaster || question != SlaveSessionQuestion.WantsToJoin)
            {
                return;
            }
            s_slaveSessionRequestPacket.Send(new(SlaveRequest.WantsToJoin, PasswordForJoinOtherLobby), SNet.Master);
        }

        public static bool OnSlaveQuestionOverride(pSlaveQuestion data)
        {
            if (!SNet.IsMaster || !SNet.LocalPlayer.IsInSessionHub || data.question != SlaveSessionQuestion.WantsToJoin)
                return true;

            if (!SNet.Core.TryGetPlayer(SNet.Replication.LastSenderID, out var player, true))
                return false;
            if (player.IsLocal)
                return true;
            if (!IsPlayerAllowedToJoinLobby(player, out var reason))
            {
                s_lobbySettingsAnswerPacket.Send(new(CurrentSettings.Privacy, MasterAnswer.LeaveLobby, reason), player);
                SNet.SessionHub.RemovePlayerFromSession(player, true);
                return false;
            }
            s_lobbySettingsAnswerPacket.Send(new(CurrentSettings.Privacy, MasterAnswer.AllowToJoin, reason), player);
            return true;
        }

        public static void DoPlayerLeftCleanup(SNet_Player player)
        {
            if (player.IsLocal)
            {
                s_tempWhitelist.Clear();
                s_receivedSlaveRequestsLookup.Clear();
                return;
            }
            s_tempWhitelist.Remove(player.Lookup);
        }


        public static void WhitelistPlayer(ulong steamid)
        {
            s_tempWhitelist.Add(steamid);
        }

        public static bool IsPlayerAllowedToJoinLobby(SNet_Player player, out MasterAnswerReason reason)
        {
            var playerName = player.GetName();
            if (PlayerLobbyManagement.IsPlayerBanned(player.Lookup))
            {
                reason = MasterAnswerReason.Banned;
                Logger.Notice($"Player {playerName} failed to join. Reason: {reason}.");
                return false;
            }

            if (s_tempWhitelist.Contains(player.Lookup))
            {
                reason = MasterAnswerReason.Whitelist;
                return true;
            }

            switch (CurrentSettings.Privacy)
            {
                case LobbyPrivacy.Invisible:
                    reason = MasterAnswerReason.InvisibleLobby;
                    Logger.Notice($"Player {playerName} failed to join. Reason: {reason}.");
                    return false;
                case LobbyPrivacy.FriendsOnly:
                    if (!player.IsFriend())
                    {
                        reason = MasterAnswerReason.IsNotFriend;
                        Logger.Notice($"Player {playerName} failed to join. Reason: {reason}.");
                        return false;
                    }
                    reason = MasterAnswerReason.IsFriend;
                    return true;
                case LobbyPrivacy.Private:
                    if (!CurrentSettings.HasPassword)
                    {
                        reason = MasterAnswerReason.Public;
                        return true;
                    }
                    if (s_receivedSlaveRequestsLookup.TryGetValue(player.Lookup, out var request) && request.Password == CurrentSettings.Password)
                    {
                        reason = MasterAnswerReason.PasswordMatch;
                        return true;
                    }
                    reason = MasterAnswerReason.PasswordMismatch;
                    Logger.Notice($"Player {playerName} failed to join. Reason: {reason}. Lobby: {CurrentSettings.Password}, Slave: {request.Password}");
                    return false;
                case LobbyPrivacy.Public:
                    reason = MasterAnswerReason.Public;
                    return true;
                default:
                    reason = MasterAnswerReason.None;
                    return true;
            }
        }

        public enum SlaveRequest : byte
        {
            WantsToJoin,
            Leaving
        }

        public enum MasterAnswer : byte
        {
            LeaveLobby,
            AllowToJoin,
        }

        [Flags]
        public enum MasterAnswerReason
        {
            None,
            Public,
            Banned,
            PasswordMismatch,
            IsNotFriend,
            InvisibleLobby,
            PasswordMatch,
            IsFriend,
            Whitelist
        }

        public struct pSlaveRequest
        {
            public pSlaveRequest(SlaveRequest question, string password = "")
            {
                Request = question;
                password ??= string.Empty;
                Password = password[..Math.Min(password.Length, 25)];
            }

            public SlaveRequest Request;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 25)]
            public string Password;
        }

        public struct pLobbyMasterAnswer
        {
            public pLobbyMasterAnswer(LobbyPrivacy privacy, MasterAnswer answer, MasterAnswerReason reason)
            {
                LobbyPrivacy = privacy;
                Answer = answer;
                Reason = reason;
            }

            public LobbyPrivacy LobbyPrivacy;
            public MasterAnswer Answer;
            public MasterAnswerReason Reason;
        }

        public class LobbySettings
        {
            public string LobbyName { get; set; }
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
                    HasPassword = !string.IsNullOrEmpty(_password);
                }
            }
            public bool HasPassword { get; private set; }
            public LobbyPrivacy Privacy = LobbyPrivacy.Public;
            public LobbyType Type => ToSNetLobbyType(Privacy);

            public const int PASSWORD_MAX_LENGTH = 25;

            private string _password = string.Empty;
        }

        public static LobbyType ToSNetLobbyType(LobbyPrivacy privacy)
        {
            return privacy switch
            {
                LobbyPrivacy.Public => LobbyType.Public,
                LobbyPrivacy.Private => LobbyType.Private,
                LobbyPrivacy.FriendsOnly => LobbyType.FriendsOnly,
                LobbyPrivacy.Invisible => LobbyType.Invisible,
                _ => LobbyType.Invisible,
            };
        }
    }
}
