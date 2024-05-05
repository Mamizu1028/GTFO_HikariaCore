using Hikaria.Core.Features.Core;
using Hikaria.Core.Interfaces;
using SNetwork;
using static Hikaria.Core.Features.Core.GameEventListener;

namespace Hikaria.Core;

public static class GameEventAPI
{
    public static event Action<eBufferType> OnRecallComplete { add => GameEventListener.OnRecallComplete += value; remove => GameEventListener.OnRecallComplete -= value; }

    public static event Action OnGameDataInited { add => GameEventListener.OnGameDataInited += value; remove => GameEventListener.OnGameDataInited -= value; }

    public static event Action<eGameStateName, eGameStateName> OnGameStateChanged { add => GameEventListener.OnGameStateChanged += value; remove => GameEventListener.OnGameStateChanged -= value; }

    public static event Action<SNet_Player, string> OnReceiveChatMessage { add => GameEventListener.OnReceiveChatMessage += value; remove => GameEventListener.OnReceiveChatMessage -= value; }

    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent { add => GameEventListener.OnPlayerEvent += value; remove => GameEventListener.OnPlayerEvent -= value; }

    public static event Action<SNet_Player, SessionMemberEvent> OnSessionMemberChanged { add => GameEventListener.OnSessionMemberChanged += value; remove => GameEventListener.OnSessionMemberChanged -= value; }

    public static void RegisterSelf<T>(T instance)
    {
        Type type = typeof(T);
        if (type.IsInterface || type.IsAbstract)
            return;
        if (typeof(IOnGameStateChanged).IsAssignableFrom(type))
            GameStateChangeListeners.Add((IOnGameStateChanged)instance);
        if (typeof(IOnPlayerEvent).IsAssignableFrom(type))
            PlayerEventListeners.Add((IOnPlayerEvent)instance);
        if (typeof(IOnReceiveChatMessage).IsAssignableFrom(type))
            ChatMessageListeners.Add((IOnReceiveChatMessage)instance);
        if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type))
            SessionMemberChangeListeners.Add((IOnSessionMemberChanged)instance);
        if (typeof(IOnRecallComplete).IsAssignableFrom(type))
            OnRecallCompleteListeners.Add((IOnRecallComplete)instance);
        if (typeof(IPauseable).IsAssignableFrom(type))
            Managers.PauseManager.RegisterPauseable((IPauseable)instance);
    }

    public static void UnregisterSelf<T>(T instance)
    {
        Type type = typeof(T);
        if (type.IsInterface || type.IsAbstract)
            return;
        if (typeof(IOnGameStateChanged).IsAssignableFrom(type))
            GameStateChangeListeners.Remove((IOnGameStateChanged)instance);
        if (typeof(IOnPlayerEvent).IsAssignableFrom(type))
            PlayerEventListeners.Remove((IOnPlayerEvent)instance);
        if (typeof(IOnReceiveChatMessage).IsAssignableFrom(type))
            ChatMessageListeners.Remove((IOnReceiveChatMessage)instance);
        if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type))
            SessionMemberChangeListeners.Remove((IOnSessionMemberChanged)instance);
        if (typeof(IOnRecallComplete).IsAssignableFrom(type))
            OnRecallCompleteListeners.Remove((IOnRecallComplete)instance);
        if (typeof(IPauseable).IsAssignableFrom(type))
            Managers.PauseManager.UnregisterPauseable((IPauseable)instance);
    }

    public static bool IsPaused { get => Managers.PauseManager.IsPaused; set => Managers.PauseManager.IsPaused = value; }
}
