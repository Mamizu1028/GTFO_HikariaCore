using Hikaria.Core.Interfaces;
using Hikaria.Core.Utility;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Interfaces;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
[DoNotSaveToConfig]
internal class GameEventListener : Feature
{
    public override string Name => "游戏事件监听";

    public override string Description => "负责游戏事件的钩子。\n属于核心功能，插件的正常运作离不开该功能。";

    public override FeatureGroup Group => EntryPoint.Groups.Dev;

    public static new IArchiveLogger FeatureLogger { get; set; }

    public override void Init()
    {
        EasyDetour.CreateAndApply<SNet_PlayerSlotManager__Internal_ManageSlot__NativeDetour>(out _);
    }

    private unsafe class SNet_PlayerSlotManager__Internal_ManageSlot__NativeDetour : EasyDetourBase<SNet_PlayerSlotManager__Internal_ManageSlot__NativeDetour.Internal_ManageSlotDel>
    {
        public delegate bool Internal_ManageSlotDel(IntPtr instancePtr, IntPtr playerPtr, IntPtr slotPtr, IntPtr slotsPtr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo);

        public override Internal_ManageSlotDel DetourTo => Detour;

        public override DetourDescriptor Descriptor => new()
        {
            Type = typeof(SNet_PlayerSlotManager),
            MethodName = nameof(SNet_PlayerSlotManager.Internal_ManageSlot),
            ArgTypes = new Type[] { typeof(SNet_Player), typeof(SNet_Slot), typeof(Il2CppReferenceArray<SNet_Slot>), typeof(SNet_SlotType), typeof(SNet_SlotHandleType), typeof(int) },
            ReturnType = typeof(bool),
            IsGeneric = false
        };

        private bool Detour(IntPtr instancePtr, IntPtr playerPtr, IntPtr slotPtr, IntPtr slotsPtr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo)
        {
            var result = Original(instancePtr, playerPtr, slotPtr, slotsPtr, type, handle, index, methodInfo);
            OnPlayerSlotChangedM(new SNet_Player(playerPtr), type, handle, index);
            return result;
        }
    }

    [ArchivePatch(typeof(SNet_GlobalManager), nameof(SNet_GlobalManager.Setup))]
    private class SNet_GlobalManager__Setup__Patch
    {
        private static void Postfix()
        {
            SNet_Events.OnMasterCommand += new Action<pMasterCommand>(OnMasterCommandM);
            SNet_Events.OnPlayerEvent += new Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason>(OnPlayerEventM);
            SNet_Events.OnRecallComplete += new Action<eBufferType>(OnRecallCompleteM);
            SNet_Events.OnMasterChanged += new Action(OnMasterChangedM);
            SNet_Events.OnPrepareForRecall += new Action<eBufferType>(OnPrepareForRecallM);
            SNet_Events.OnResetSessionEvent += new Action(OnResetSessionM);
        }
    }

    [ArchivePatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
    private class GS_AfterLevel__CleanupAfterExpedition__Patch
    {
        private static void Postfix()
        {
            CleanupAfterExpeditionM();
        }
    }

    [ArchivePatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnRecallDone))]
    private class SNet_SyncManager__OnRecallDone__Patch
    {
        private static void Postfix(eBufferType bufferType)
        {
            OnRecallDoneM(bufferType);
        }
    }

    [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.AddPlayerToSession))]
    private class SNet_SessionHub__AddPlayerToSession__Patch
    {
        private static void Postfix(SNet_Player player)
        {
            OnSessionMemberChangedM(player, SessionMemberEvent.JoinSessionHub);
        }
    }

    [ArchivePatch(typeof(GameStateManager), nameof(GameStateManager.DoChangeState))]
    private class GameStateManager__DoChangeState__Patch
    {
        private static eGameStateName preState;

        private static void Prefix(GameStateManager __instance)
        {
            preState = __instance.m_currentStateName;
        }

        private static void Postfix(eGameStateName nextState)
        {
            foreach (var listener in GameStateChangeListeners)
            {
                try
                {
                    listener.OnGameStateChanged(preState, nextState);
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }
            }
            try
            {
                var onGameStateChanged = OnGameStateChanged;
                onGameStateChanged?.Invoke(preState, nextState);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
    }

    [ArchivePatch(typeof(PlayerChatManager), nameof(PlayerChatManager.DoSendChatMessage))]
    private class PlayerCharManager__DoSendChatMessage__Patch
    {
        private static void Postfix(PlayerChatManager.pChatMessage data)
        {
            if (data.fromPlayer.TryGetPlayer(out SNet_Player fromPlayer))
            {
                foreach (var listener in ChatMessageListeners)
                {
                    try
                    {
                        listener.OnReceiveChatMessage(fromPlayer, data.message.data);
                    }
                    catch (Exception ex)
                    {
                        FeatureLogger.Exception(ex);
                    }
                }
                try
                {
                    var onReceiveChatMessage = OnReceiveChatMessage;
                    onReceiveChatMessage?.Invoke(fromPlayer, data.message.data);
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }
            }
        }
    }

    private static void CleanupAfterExpeditionM()
    {
        foreach (var listener in AfterLevelCleanupListeners)
        {
            try
            {
                listener.OnAfterLevelCleanup();
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onAfterLevelCleanup = OnAfterLevelCleanup;
            onAfterLevelCleanup?.Invoke();
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }

    private static void OnMasterChangedM()
    {
        foreach (var listener in MasterChangedListeners)
        {
            try
            {
                listener.OnMasterChanged();
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onMasterChanged = OnMasterChanged;
            onMasterChanged?.Invoke();
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }

    private static void OnPrepareForRecallM(eBufferType bufferType)
    {
        foreach (var listener in PrepareForRecallListeners)
        {
            try
            {
                listener.OnPrepareForRecall(bufferType);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onPrepareForRecall = OnPrepareForRecall;
            onPrepareForRecall?.Invoke(bufferType);
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }

    private static void OnRecallDoneM(eBufferType bufferType)
    {
        foreach (var listener in RecallDoneListeners)
        {
            try
            {
                listener.OnRecallDone(bufferType);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onRecallDone = OnRecallDone;
            onRecallDone?.Invoke(bufferType);
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }

    private static void OnRecallCompleteM(eBufferType bufferType)
    {
        foreach (var listener in RecallCompleteListeners)
        {
            try
            {
                listener.OnRecallComplete(bufferType);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onRecallComplete = OnRecallComplete;
            onRecallComplete?.Invoke(bufferType);
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }

    private static void OnPlayerEventM(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason)
    {
        foreach (var Listener in PlayerEventListeners)
        {
            try
            {
                Listener.OnPlayerEvent(player, playerEvent, reason);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }

        try
        {
            var onPlayerEvent = OnPlayerEvent;
            onPlayerEvent?.Invoke(player, playerEvent, reason);
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }

        switch (playerEvent)
        {
            case SNet_PlayerEvent.PlayerLeftSessionHub:
                OnSessionMemberChangedM(player, SessionMemberEvent.LeftSessionHub);
                break;
        }
    }

    private static void OnPlayerSlotChangedM(SNet_Player player, SNet_SlotType type, SNet_SlotHandleType handle, int index)
    {
        foreach (var Listener in PlayerSlotChangedListener)
        {
            try
            {
                Listener.OnPlayerSlotChanged(player, type, handle, index);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }

        try
        {
            var onPlayerSlotChanged = OnPlayerSlotChanged;
            onPlayerSlotChanged?.Invoke(player, type, handle, index);
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }


    private static void OnMasterCommandM(pMasterCommand command)
    {
        foreach (var Listener in MasterCommandListeners)
        {
            try
            {
                Listener.OnMasterCommand(command.type, command.refA);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onMasterCommand = OnMasterCommand;
            onMasterCommand?.Invoke(command.type, command.refA);
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }


    private static void OnSessionMemberChangedM(SNet_Player player, SessionMemberEvent playerEvent)
    {
        FeatureLogger.Notice($"{player.NickName} [{player.Lookup}] {playerEvent}");
        foreach (var Listener in SessionMemberChangedListeners)
        {
            try
            {
                Listener.OnSessionMemberChanged(player, playerEvent);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onSessionMemberChanged = OnSessionMemberChanged;
            if (onSessionMemberChanged != null)
            {
                onSessionMemberChanged(player, playerEvent);
            }
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }

    private static void OnResetSessionM()
    {
        foreach (var Listener in ResetSessionListeners)
        {
            try
            {
                Listener.OnResetSession();
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
        try
        {
            var onResetSession = OnResetSession;
            onResetSession?.Invoke();
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }


    public static void RegisterSelf<T>(T instance)
    {
        Type type = instance.GetType();
        if (type.IsInterface || type.IsAbstract)
            return;
        if (typeof(IOnGameStateChanged).IsAssignableFrom(type))
            GameStateChangeListeners.Add((IOnGameStateChanged)instance);
        if (typeof(IOnPlayerEvent).IsAssignableFrom(type))
            PlayerEventListeners.Add((IOnPlayerEvent)instance);
        if (typeof(IOnReceiveChatMessage).IsAssignableFrom(type))
            ChatMessageListeners.Add((IOnReceiveChatMessage)instance);
        if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type))
            SessionMemberChangedListeners.Add((IOnSessionMemberChanged)instance);
        if (typeof(IOnRecallComplete).IsAssignableFrom(type))
            RecallCompleteListeners.Add((IOnRecallComplete)instance);
        if (typeof(IOnPrepareForRecall).IsAssignableFrom(type))
            PrepareForRecallListeners.Add((IOnPrepareForRecall)instance);
        if (typeof(IOnRecallDone).IsAssignableFrom(type))
            RecallDoneListeners.Add((IOnRecallDone)instance);
        if (typeof(IOnMasterChanged).IsAssignableFrom(type))
            MasterChangedListeners.Add((IOnMasterChanged)instance);
        if (typeof(IOnMasterCommand).IsAssignableFrom(type))
            MasterCommandListeners.Add((IOnMasterCommand)instance);
        if (typeof(IOnPlayerSlotChanged).IsAssignableFrom(type))
            PlayerSlotChangedListener.Add((IOnPlayerSlotChanged)instance);
        if (typeof(IOnAfterLevelCleanup).IsAssignableFrom(type))
            AfterLevelCleanupListeners.Add((IOnAfterLevelCleanup)instance);
        if (typeof(IOnResetSession).IsAssignableFrom(type))
            ResetSessionListeners.Add((IOnResetSession)instance);
        if (typeof(IPauseable).IsAssignableFrom(type))
            Managers.PauseManager.RegisterPauseable((IPauseable)instance);
    }

    public static void UnregisterSelf<T>(T instance)
    {
        Type type = instance.GetType();
        if (type.IsInterface || type.IsAbstract)
            return;
        if (typeof(IOnGameStateChanged).IsAssignableFrom(type))
            GameStateChangeListeners.Remove((IOnGameStateChanged)instance);
        if (typeof(IOnPlayerEvent).IsAssignableFrom(type))
            PlayerEventListeners.Remove((IOnPlayerEvent)instance);
        if (typeof(IOnReceiveChatMessage).IsAssignableFrom(type))
            ChatMessageListeners.Remove((IOnReceiveChatMessage)instance);
        if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type))
            SessionMemberChangedListeners.Remove((IOnSessionMemberChanged)instance);
        if (typeof(IOnRecallComplete).IsAssignableFrom(type))
            RecallCompleteListeners.Remove((IOnRecallComplete)instance);
        if (typeof(IOnPrepareForRecall).IsAssignableFrom(type))
            PrepareForRecallListeners.Remove((IOnPrepareForRecall)instance);
        if (typeof(IOnRecallDone).IsAssignableFrom(type))
            RecallDoneListeners.Remove((IOnRecallDone)instance);
        if (typeof(IOnMasterChanged).IsAssignableFrom(type))
            MasterChangedListeners.Remove((IOnMasterChanged)instance);
        if (typeof(IOnMasterCommand).IsAssignableFrom(type))
            MasterCommandListeners.Remove((IOnMasterCommand)instance);
        if (typeof(IOnPlayerSlotChanged).IsAssignableFrom(type))
            PlayerSlotChangedListener.Remove((IOnPlayerSlotChanged)instance);
        if (typeof(IOnAfterLevelCleanup).IsAssignableFrom(type))
            AfterLevelCleanupListeners.Remove((IOnAfterLevelCleanup)instance);
        if (typeof(IOnResetSession).IsAssignableFrom(type))
            ResetSessionListeners.Remove((IOnResetSession)instance);
        if (typeof(IPauseable).IsAssignableFrom(type))
            Managers.PauseManager.UnregisterPauseable((IPauseable)instance);
    }

    private static HashSet<IOnGameStateChanged> GameStateChangeListeners = new();
    private static HashSet<IOnReceiveChatMessage> ChatMessageListeners = new();
    private static HashSet<IOnPlayerEvent> PlayerEventListeners = new();
    private static HashSet<IOnRecallComplete> RecallCompleteListeners = new();
    private static HashSet<IOnSessionMemberChanged> SessionMemberChangedListeners = new();
    private static HashSet<IOnMasterChanged> MasterChangedListeners = new();
    private static HashSet<IOnMasterCommand> MasterCommandListeners = new();
    private static HashSet<IOnPlayerSlotChanged> PlayerSlotChangedListener = new();
    private static HashSet<IOnPrepareForRecall> PrepareForRecallListeners = new();
    private static HashSet<IOnRecallDone> RecallDoneListeners = new();
    private static HashSet<IOnAfterLevelCleanup> AfterLevelCleanupListeners = new();
    private static HashSet<IOnResetSession> ResetSessionListeners = new();

    public static event Action OnGameDataInited;
    public static event Action<eBufferType> OnRecallComplete;
    public static event Action<eBufferType> OnPrepareForRecall;
    public static event Action<eBufferType> OnRecallDone;
    public static event Action<eGameStateName, eGameStateName> OnGameStateChanged;
    public static event Action<SNet_Player, string> OnReceiveChatMessage;
    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent;
    public static event Action<SNet_Player, SessionMemberEvent> OnSessionMemberChanged;
    public static event Action OnMasterChanged;
    public static event Action<eMasterCommandType, int> OnMasterCommand;
    public static event Action<SNet_Player, SNet_SlotType, SNet_SlotHandleType, int> OnPlayerSlotChanged;
    public static event Action OnAfterLevelCleanup;
    public static event Action OnResetSession;
}