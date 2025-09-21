using Hikaria.Core.Features.Dev;
using SNetwork;

namespace Hikaria.Core;

public static class GameEventAPI
{
    public static event Action<eBufferType> OnRecallComplete { add => GameEventAPI_Impl.OnRecallComplete += value; remove => GameEventAPI_Impl.OnRecallComplete -= value; }
    public static event Action OnGameDataInited { add => GameEventAPI_Impl.OnGameDataInited += value; remove => GameEventAPI_Impl.OnGameDataInited -= value; }
    public static event Action<eGameStateName, eGameStateName> OnGameStateChanged { add => GameEventAPI_Impl.OnGameStateChanged += value; remove => GameEventAPI_Impl.OnGameStateChanged -= value; }
    public static event Action<SNet_Player, string> OnReceiveChatMessage { add => GameEventAPI_Impl.OnReceiveChatMessage += value; remove => GameEventAPI_Impl.OnReceiveChatMessage -= value; }
    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent { add => GameEventAPI_Impl.OnPlayerEvent += value; remove => GameEventAPI_Impl.OnPlayerEvent -= value; }
    public static event Action<SNet_Player, SessionMemberEvent> OnSessionMemberChanged { add => GameEventAPI_Impl.OnSessionMemberChanged += value; remove => GameEventAPI_Impl.OnSessionMemberChanged -= value; }
    public static event Action OnMasterChanged { add => GameEventAPI_Impl.OnMasterChanged += value; remove => GameEventAPI_Impl.OnMasterChanged -= value; }
    public static event Action<eMasterCommandType, int> OnMasterCommand { add => GameEventAPI_Impl.OnMasterCommand += value; remove => GameEventAPI_Impl.OnMasterCommand -= value; }
    public static event Action OnAfterLevelCleanup { add => GameEventAPI_Impl.OnAfterLevelCleanup += value; remove => GameEventAPI_Impl.OnAfterLevelCleanup -= value; }
    public static event Action OnResetSession { add => GameEventAPI_Impl.OnResetSession += value; remove => GameEventAPI_Impl.OnResetSession -= value; }
    public static event Action<eBufferType> OnRecallDone { add => GameEventAPI_Impl.OnRecallDone += value; remove => GameEventAPI_Impl.OnRecallDone -= value; }
    public static event Action<eBufferType> OnPrepareForRecall { add => GameEventAPI_Impl.OnPrepareForRecall += value; remove => GameEventAPI_Impl.OnPrepareForRecall -= value; }
    public static event Action<pBufferCommand> OnBufferCommand { add => GameEventAPI_Impl.OnBufferCommand += value; remove => GameEventAPI_Impl.OnBufferCommand -= value; }

    public static void RegisterListener<T>(T instance)
    {
        GameEventAPI_Impl.RegisterListener(instance);
    }

    public static void UnregisterListener<T>(T instance)
    {
        GameEventAPI_Impl.UnregisterListener(instance);
    }

    public static bool IsGamePaused { get => global::PauseManager.IsPaused; set => global::PauseManager.IsPaused = value; }
    public static event Action OnGamePaused { add => Managers.PauseManager.OnPaused += value; remove => Managers.PauseManager.OnPaused -= value; }
    public static event Action OnGameUnpaused { add => Managers.PauseManager.OnUnpaused += value; remove => Managers.PauseManager.OnUnpaused -= value; }
}
