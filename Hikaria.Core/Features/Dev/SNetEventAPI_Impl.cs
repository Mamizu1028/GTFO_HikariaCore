using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Interfaces;
using TheArchive.Utilities;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
[DoNotSaveToConfig]
internal class SNetEventAPI_Impl : Feature
{
    public override string Name => "SNetworkAPI Impl";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public static new IArchiveLogger FeatureLogger { get; set; }

    #region Events
    public static event Action<pBufferCommand> OnBufferCommand;
    public static event Action<eBufferType> OnBufferCapture;
    public static event Action<eBufferType> OnPrepareForRecall;
    public static event Action<eBufferType> OnBufferRecalled;
    public static event Action<eBufferType> OnRecallDone;
    public static event Action<eBufferType> OnRecallComplete;
    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent;
    public static event Action<SNet_Player, SessionMemberEvent> OnSessionMemberChanged;
    public static event Action OnMasterChanged;
    public static event Action<eMasterCommandType, int> OnMasterCommand;
    public static event Action OnResetSession;
    #endregion

    [ArchivePatch(typeof(SNet_GlobalManager), nameof(SNet_GlobalManager.Setup))]
    private class SNet_GlobalManager__Setup__Patch
    {
        private static void Postfix()
        {
            SNet_Events.OnMasterCommand += new Action<pMasterCommand>((command) => Utils.SafeInvoke(OnMasterCommand, command));
            SNet_Events.OnPlayerEvent += new Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason>((player, playerEvent, reason) => {
                Utils.SafeInvoke(OnPlayerEvent, player, playerEvent, reason);
                if (playerEvent == SNet_PlayerEvent.PlayerLeftSessionHub)
                {
                    FeatureLogger.Notice($"{player.NickName} [{player.Lookup}] {playerEvent}");
                    Utils.SafeInvoke(OnSessionMemberChanged, player, SessionMemberEvent.LeftSessionHub);
                }
            });
            SNet_Events.OnRecallComplete += new Action<eBufferType>((buffer) => Utils.SafeInvoke(OnRecallComplete, buffer));
            SNet_Events.OnMasterChanged += new Action(() => Utils.SafeInvoke(OnMasterChanged));
            SNet_Events.OnPrepareForRecall += new Action<eBufferType>((buffer) => Utils.SafeInvoke(OnPrepareForRecall, buffer));
            SNet_Events.OnResetSessionEvent += new Action(() => Utils.SafeInvoke(OnResetSession));
        }
    }

    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.TriggerCapture))]
    private class SNet_Capture__TriggerCapture__Patch
    {
        private static void Prefix(SNet_Capture __instance)
        {
            Utils.SafeInvoke(OnBufferCapture, __instance.PrimedBufferType);
        }
    }

    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.RecallBuffer))]
    private class SNet_Capture__RecallBuffer__Patch
    {
        static void Postfix(SNet_Capture __instance, eBufferType bufferType)
        {
            if (__instance.IsRecalling) return;

            Utils.SafeInvoke(OnBufferRecalled, bufferType);
        }
    }

    [ArchivePatch(typeof(SNet_Capture), nameof(SNet_Capture.OnBufferCommand))]
    private class SNet_Capture__OnBufferCommand__Patch
    {
        private static void Postfix(pBufferCommand command)
        {
            Utils.SafeInvoke(OnBufferCommand, command);
        }
    }

    [ArchivePatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnRecallDone))]
    private class SNet_SyncManager__OnRecallDone__Patch
    {
        private static void Postfix(eBufferType bufferType)
        {
            Utils.SafeInvoke(OnRecallDone, bufferType);
        }
    }

    [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.AddPlayerToSession))]
    private class SNet_SessionHub__AddPlayerToSession__Patch
    {
        private static void Postfix(SNet_Player player)
        {
            Utils.SafeInvoke(OnSessionMemberChanged, player, SessionMemberEvent.JoinSessionHub);
        }
    }
}
