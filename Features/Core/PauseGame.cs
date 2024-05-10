using Globals;
using LevelGeneration;
using Player;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using UnityEngine;

namespace Hikaria.Core.Features.Core;

[DisallowInGameToggle]
[EnableFeatureByDefault]
[DoNotSaveToConfig]
public class PauseGame : Feature
{
    public override string Name => "Pause Game";

    public override FeatureGroup Group => EntryPoint.Groups.Core;

    [FeatureConfig]
    public static PauseGameSettings Settings { get; set; }

    public class PauseGameSettings
    {
        [FSDisplayName("当前状态")]
        public PauseGameStatus CurrentStatus
        {
            get => GameEventAPI.IsGamePaused ? PauseGameStatus.Paused : PauseGameStatus.Unpaused;
            set
            {
                if (!SNet.IsMaster || CurrentGameState != (int)eGameStateName.InLevel) return;

                GameEventAPI.IsGamePaused = value == PauseGameStatus.Paused;
            }
        }
    }

    [Localized]
    public enum PauseGameStatus
    {
        Unpaused,
        Paused
    }

    public override void OnGameStateChanged(int _)
    {
        GameEventAPI.IsGamePaused = false;
    }

    [ArchivePatch(typeof(GS_InLevel), nameof(GS_InLevel.Update))]
    private class GS_InLevel__Update__Postfix
    {
        private static void Postfix()
        {
            if (GameEventAPI.IsGamePaused)
            {
                Clock.ExpeditionProgressionTime = ExpeditionProgressionTime;
            }
        }
    }

    [ArchivePatch(typeof(Global), nameof(Global.OnPaused))]
    private class Global__OnPaused__Patch
    {
        private static void Postfix()
        {
            Managers.PauseManager.IsPaused = true;
            DoPauseGame(true);
        }
    }

    [ArchivePatch(typeof(Global), nameof(Global.OnUnpaused))]
    private class Global__SetUnpaused__Patch
    {
        private static void Postfix()
        {
            Managers.PauseManager.IsPaused = false;
            DoPauseGame(false);
        }
    }

    [ArchivePatch(typeof(PlayerSync), nameof(PlayerSync.IncomingLocomotion))]
    private class PlayerSync__IncomongLocomotion__Patch
    {
        private static void Postfix(PlayerSync __instance, pPlayerLocomotion data)
        {
            if (!SNet.IsMaster || !GameEventAPI.IsGamePaused || __instance.m_agent.IsLocallyOwned)
            {
                return;
            }
            var isReady = __instance.m_agent?.Owner?.Load<pReady>().isReady ?? false;
            if (!isReady)
            {
                return;
            }
            var player = __instance.m_agent.Owner;
            if (player.IsOutOfSync)
            {
                return;
            }
            if (Vector3.Distance(data.Pos, __instance.m_agent.Position) >= 2f)
            {
                __instance.m_agent.RequestToggleControlsEnabled(false);
            }
            if (__instance.m_agent.Inventory.WieldedSlot != InventorySlot.None)
            {
                __instance.m_agent.RequestToggleControlsEnabled(false);
                __instance.WantsToWieldSlot(InventorySlot.None);
            }
        }
    }

    private static float ExpeditionProgressionTime;

    private static void DoPauseGame(bool pause)
    {
        if (pause)
        {
            ExpeditionProgressionTime = Clock.ExpeditionProgressionTime;
        }
        else
        {
            Clock.ExpeditionProgressionTime = ExpeditionProgressionTime;
        }
        SetPauseForWardenObjectiveItems(pause);
        SetPauseForAllPlayers(pause);
    }

    private static void SetPauseForAllPlayers(bool paused)
    {
        foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
        {
            if (paused)
            {
                player.Sync.WantsToWieldSlot(InventorySlot.None);
            }
            player.RequestToggleControlsEnabled(!paused);
        }
    }

    private static void SetPauseForWardenObjectiveItems(bool paused)
    {
        var reactors = UnityEngine.Object.FindObjectsOfType<LG_WardenObjective_Reactor>();
        foreach (var reactor in reactors)
        {
            if (reactor.m_isWardenObjective && !reactor.ObjectiveItemSolved)
            {
                switch (reactor.m_currentState.status)
                {
                    case eReactorStatus.Startup_intro:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                    case eReactorStatus.Startup_intense:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                    case eReactorStatus.Startup_waitForVerify:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                    case eReactorStatus.Shutdown_intro:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                }
            }
        }
    }
}
