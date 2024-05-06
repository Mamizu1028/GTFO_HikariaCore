using BepInEx.Unity.IL2CPP.Hook;
using Hikaria.Core.Interfaces;
using Hikaria.Core.Utilities;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Runtime;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Interfaces;

namespace Hikaria.Core.Features.Core;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
[DoNotSaveToConfig]
internal class GameEventListener : Feature
{
    public override string Name => "Game Event Listener";

    public override FeatureGroup Group => EntryPoint.Groups.Core;

    public static new IArchiveLogger FeatureLogger { get; set; }

    public override void Init()
    {
        SNet_PlayerSlotManager__Internal_ManageSlot__NativeDetour.ApplyDetour();
        SNet_SessionHub__AddPlayerToSession__NativeDetour.ApplyDetour();
        SNet_SessionHub__RemovePlayerFromSession__NativeDetour.ApplyDetour();
    }

    private static class SNet_PlayerSlotManager__Internal_ManageSlot__NativeDetour
    {
        private unsafe delegate bool Internal_ManageSlotDel(IntPtr instancePtr, IntPtr playerPtr, IntPtr slotPtr, IntPtr slotsPtr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo);

        private static Internal_ManageSlotDel _Original;

        private static INativeDetour _Detour;

        public static unsafe void ApplyDetour()
        {
            DetourDescriptor desc = new()
            {
                Type = typeof(SNet_PlayerSlotManager),
                MethodName = nameof(SNet_PlayerSlotManager.Internal_ManageSlot),
                ArgTypes = new Type[] { typeof(SNet_Player), typeof(SNet_Slot), typeof(Il2CppReferenceArray<SNet_Slot>), typeof(SNet_SlotType), typeof(SNet_SlotHandleType), typeof(int) },
                ReturnType = typeof(bool),
                IsGeneric = false
            };
            EasyDetour.TryCreate(desc, Detour, out _Original, out _Detour);
        }

        private static unsafe bool Detour(IntPtr instancePtr, IntPtr playerPtr, IntPtr slotPtr, IntPtr slotsPtr, SNet_SlotType type, SNet_SlotHandleType handle, int index, Il2CppMethodInfo* methodInfo)
        {
            var result = _Original(instancePtr, playerPtr, slotPtr, slotsPtr, type, handle, index, methodInfo);
            OnPlayerSlotChangedM(new SNet_Player(playerPtr), type, handle, index);
            return result;
        }
    }

    private static class SNet_SessionHub__AddPlayerToSession__NativeDetour
    {
        private unsafe delegate void AddPlayerToSessionDel(IntPtr instancePtr, IntPtr playerPtr, bool broadcastIfMaster, Il2CppMethodInfo* methodInfo);

        private static AddPlayerToSessionDel _Original;

        private static INativeDetour _Detour;

        public static unsafe void ApplyDetour()
        {
            DetourDescriptor desc = new()
            {
                Type = typeof(SNet_SessionHub),
                MethodName = nameof(SNet_SessionHub.AddPlayerToSession),
                ArgTypes = new Type[] { typeof(SNet_Player), typeof(bool) },
                ReturnType = typeof(void),
                IsGeneric = false
            };
            EasyDetour.TryCreate(desc, Detour, out _Original, out _Detour);
        }

        private static void Unpatch()
        {
            _Detour.Undo();
            _Detour.Free();
            _Detour.Dispose();
        }

        private static unsafe void Detour(IntPtr instancePtr, IntPtr playerPtr, bool broadcastIfMaster, Il2CppMethodInfo* methodInfo)
        {
            _Original(instancePtr, playerPtr, broadcastIfMaster, methodInfo);
            OnSessionMemberChangedM(new SNet_Player(playerPtr), SessionMemberEvent.JoinSessionHub);
        }
    }

    private static class SNet_SessionHub__RemovePlayerFromSession__NativeDetour
    {
        private unsafe delegate void RemovePlayerFromSessionDel(IntPtr instancePtr, IntPtr playerPtr, bool broadcastIfMaster, Il2CppMethodInfo* methodInfo);

        private static RemovePlayerFromSessionDel _Original;

        private static INativeDetour _Detour;

        public static unsafe void ApplyDetour()
        {
            DetourDescriptor desc = new()
            {
                Type = typeof(SNet_SessionHub),
                MethodName = nameof(SNet_SessionHub.RemovePlayerFromSession),
                ArgTypes = new Type[] { typeof(SNet_Player), typeof(bool) },
                ReturnType = typeof(void),
                IsGeneric = false
            };
            EasyDetour.TryCreate(desc, Detour, out _Original, out _Detour);
        }

        private static unsafe void Detour(IntPtr instancePtr, IntPtr playerPtr, bool broadcastIfMaster, Il2CppMethodInfo* methodInfo)
        {
            _Original(instancePtr, playerPtr, broadcastIfMaster, methodInfo);
            OnSessionMemberChangedM(new SNet_Player(playerPtr), SessionMemberEvent.LeftSessionHub);
        }
    }

    [ArchivePatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.LeaveHub))]
    private class SNet_SessionHub__LeaveHub__Patch
    {
        private static void Postfix()
        {
            OnSessionMemberChangedM(SNet.LocalPlayer, SessionMemberEvent.LeftSessionHub);
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
                if (onGameStateChanged != null)
                {
                    onGameStateChanged(preState, nextState);
                }
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
                    if (onReceiveChatMessage != null)
                    {
                        onReceiveChatMessage(fromPlayer, data.message.data);
                    }
                }
                catch (Exception ex)
                {
                    FeatureLogger.Exception(ex);
                }
            }
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
            if (onMasterChanged != null)
            {
                onMasterChanged();
            }
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
            if (onRecallComplete != null)
            {
                onRecallComplete(bufferType);
            }
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
            if (onPlayerEvent != null)
            {
                onPlayerEvent(player, playerEvent, reason);
            }
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
        /*
        switch (playerEvent)
        {
            case SNet_PlayerEvent.PlayerLeftSessionHub:
                OnSessionMemberChangedM(player, SessionMemberEvent.LeftSessionHub);
                break;
        }
        */
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
            if (onPlayerSlotChanged != null)
            {
                onPlayerSlotChanged(player, type, handle, index);
            }
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
            if (onMasterCommand != null)
            {
                onMasterCommand(command.type, command.refA);
            }
        }
        catch (Exception ex)
        {
            FeatureLogger.Exception(ex);
        }
    }


    private static void OnSessionMemberChangedM(SNet_Player player, SessionMemberEvent playerEvent)
    {
        FeatureLogger.Notice($"{player.NickName} [{player.Lookup}] {playerEvent}");
        foreach (var Listener in SessionMemberChangeListeners)
        {
            try
            {
                Listener.OnSessionMemberChange(player, playerEvent);
            }
            catch (Exception ex)
            {
                FeatureLogger.Exception(ex);
            }
        }
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
        if (typeof(IOnSessionMemberChange).IsAssignableFrom(type))
            SessionMemberChangeListeners.Add((IOnSessionMemberChange)instance);
        if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type))
            SessionMemberChangedListeners.Add((IOnSessionMemberChanged)instance);
        if (typeof(IOnRecallComplete).IsAssignableFrom(type))
            RecallCompleteListeners.Add((IOnRecallComplete)instance);
        if (typeof(IOnMasterChanged).IsAssignableFrom(type))
            MasterChangedListeners.Add((IOnMasterChanged)instance);
        if (typeof(IOnMasterCommand).IsAssignableFrom(type))
            MasterCommandListeners.Add((IOnMasterCommand)instance);
        if (typeof(IOnPlayerSlotChanged).IsAssignableFrom(type))
            PlayerSlotChangedListener.Add((IOnPlayerSlotChanged)instance);
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
        if (typeof(IOnSessionMemberChange).IsAssignableFrom(type))
            SessionMemberChangeListeners.Remove((IOnSessionMemberChange)instance);
        if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type))
            SessionMemberChangedListeners.Remove((IOnSessionMemberChanged)instance);
        if (typeof(IOnRecallComplete).IsAssignableFrom(type))
            RecallCompleteListeners.Remove((IOnRecallComplete)instance);
        if (typeof(IOnMasterChanged).IsAssignableFrom(type))
            MasterChangedListeners.Remove((IOnMasterChanged)instance);
        if (typeof(IOnMasterCommand).IsAssignableFrom(type))
            MasterCommandListeners.Remove((IOnMasterCommand)instance);
        if (typeof(IOnPlayerSlotChanged).IsAssignableFrom(type))
            PlayerSlotChangedListener.Remove((IOnPlayerSlotChanged)instance);
        if (typeof(IPauseable).IsAssignableFrom(type))
            Managers.PauseManager.UnregisterPauseable((IPauseable)instance);
    }

    private static HashSet<IOnGameStateChanged> GameStateChangeListeners = new();
    private static HashSet<IOnReceiveChatMessage> ChatMessageListeners = new();
    private static HashSet<IOnPlayerEvent> PlayerEventListeners = new();
    private static HashSet<IOnRecallComplete> RecallCompleteListeners = new();
    private static HashSet<IOnSessionMemberChange> SessionMemberChangeListeners = new();
    private static HashSet<IOnSessionMemberChanged> SessionMemberChangedListeners = new();
    private static HashSet<IOnMasterChanged> MasterChangedListeners = new();
    private static HashSet<IOnMasterCommand> MasterCommandListeners = new();
    private static HashSet<IOnPlayerSlotChanged> PlayerSlotChangedListener = new();

    public static event Action OnGameDataInited;
    public static event Action<eBufferType> OnRecallComplete;
    public static event Action<eGameStateName, eGameStateName> OnGameStateChanged;
    public static event Action<SNet_Player, string> OnReceiveChatMessage;
    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent;
    public static event Action<SNet_Player, SessionMemberEvent> OnSessionMemberChanged;
    public static event Action OnMasterChanged;
    public static event Action<eMasterCommandType, int> OnMasterCommand;
    public static event Action<SNet_Player, SNet_SlotType, SNet_SlotHandleType, int> OnPlayerSlotChanged;
}