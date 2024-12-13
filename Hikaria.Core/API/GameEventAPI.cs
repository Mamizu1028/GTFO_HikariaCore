using Hikaria.Core.Features.Dev;
using Hikaria.Core.Interfaces;
using SNetwork;

namespace Hikaria.Core;

public static class GameEventAPI
{
    public static event Action<eBufferType> OnRecallComplete { add => GameEventListener.OnRecallComplete += value; remove => GameEventListener.OnRecallComplete -= value; }
    public static event Action OnGameDataInited { add => GameEventListener.OnGameDataInited += value; remove => GameEventListener.OnGameDataInited -= value; }
    public static event Action<eGameStateName, eGameStateName> OnGameStateChanged { add => GameEventListener.OnGameStateChanged += value; remove => GameEventListener.OnGameStateChanged -= value; }
    public static event Action<SNet_Player, string> OnReceiveChatMessage { add => GameEventListener.OnReceiveChatMessage += value; remove => GameEventListener.OnReceiveChatMessage -= value; }
    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent { add => GameEventListener.OnPlayerEvent += value; remove => GameEventListener.OnPlayerEvent -= value; }
    public static event Action<SNet_Player, SessionMemberEvent> OnSessionMemberChanged { add => GameEventListener.OnSessionMemberChanged += value; remove => GameEventListener.OnSessionMemberChanged -= value; }
    public static event Action OnMasterChanged { add => GameEventListener.OnMasterChanged += value; remove => GameEventListener.OnMasterChanged -= value; }
    public static event Action<eMasterCommandType, int> OnMasterCommand { add => GameEventListener.OnMasterCommand += value; remove => GameEventListener.OnMasterCommand -= value; }
    public static event Action OnAfterLevelCleanup { add => GameEventListener.OnAfterLevelCleanup += value; remove => GameEventListener.OnAfterLevelCleanup -= value; }
    public static event Action OnResetSession { add => GameEventListener.OnResetSession += value; remove => GameEventListener.OnResetSession -= value; }
    public static event Action<eBufferType> OnRecallDone { add => GameEventListener.OnRecallDone += value; remove => GameEventListener.OnRecallDone -= value; }
    public static event Action<eBufferType> OnPrepareForRecall { add => GameEventListener.OnPrepareForRecall += value; remove => GameEventListener.OnPrepareForRecall -= value; }
    public static event Action<pBufferCommand> OnBufferCommand { add => GameEventListener.OnBufferCommand += value; remove => GameEventListener.OnBufferCommand -= value; }

    public static void RegisterSelf<T>(T instance)
    {
        GameEventListener.RegisterSelf(instance);
    }

    public static void UnregisterSelf<T>(T instance)
    {
        GameEventListener.UnregisterSelf(instance);
    }

    public static bool IsGamePaused { get => global::PauseManager.IsPaused; set => global::PauseManager.IsPaused = value; }
    public static event Action OnGamePaused { add => Managers.PauseManager.OnPaused += value; remove => Managers.PauseManager.OnPaused -= value; }
    public static event Action OnGameUnpaused { add => Managers.PauseManager.OnUnpaused += value; remove => Managers.PauseManager.OnUnpaused -= value; }
}
