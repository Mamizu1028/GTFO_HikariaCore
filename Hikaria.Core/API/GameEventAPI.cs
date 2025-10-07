using Hikaria.Core.Features.Dev;
using SNetwork;

namespace Hikaria.Core;

public static class GameEventAPI
{
    public static event Action OnGameDataInitialized { add => GameEventAPI_Impl.OnGameDataInitialized += value; remove => GameEventAPI_Impl.OnGameDataInitialized -= value; }
    public static event Action<eGameStateName, eGameStateName> OnGameStateChanged { add => GameEventAPI_Impl.OnGameStateChanged += value; remove => GameEventAPI_Impl.OnGameStateChanged -= value; }
    public static event Action<SNet_Player, string> OnReceiveChatMessage { add => GameEventAPI_Impl.OnReceiveChatMessage += value; remove => GameEventAPI_Impl.OnReceiveChatMessage -= value; }
    public static event Action OnAfterLevelCleanup { add => GameEventAPI_Impl.OnAfterLevelCleanup += value; remove => GameEventAPI_Impl.OnAfterLevelCleanup -= value; }

    public static bool IsGamePaused { get => global::PauseManager.IsPaused; set => global::PauseManager.IsPaused = value; }
    public static event Action OnGamePaused { add => Managers.PauseManager.OnPaused += value; remove => Managers.PauseManager.OnPaused -= value; }
    public static event Action OnGameUnpaused { add => Managers.PauseManager.OnUnpaused += value; remove => Managers.PauseManager.OnUnpaused -= value; }
}
