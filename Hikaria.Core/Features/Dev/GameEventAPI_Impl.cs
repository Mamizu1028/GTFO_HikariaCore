using GameData;
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
internal class GameEventAPI_Impl : Feature
{
    public override string Name => "游戏事件监听";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public static new IArchiveLogger FeatureLogger { get; set; }

    #region Events
    public static new event Action OnGameDataInitialized;
    public static new event Action<eGameStateName, eGameStateName> OnGameStateChanged;
    public static event Action<SNet_Player, string> OnReceiveChatMessage;
    public static event Action OnAfterLevelCleanup;
    #endregion

    [ArchivePatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
    private class GameDataInit__Initialize__Patch
    {
        private static void Postfix()
        {
            Utils.SafeInvoke(OnGameDataInitialized);
        }
    }

    [ArchivePatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
    private class GS_AfterLevel__CleanupAfterExpedition__Patch
    {
        private static void Postfix()
        {
            Utils.SafeInvoke(OnAfterLevelCleanup);
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
            Utils.SafeInvoke(OnGameStateChanged, preState, nextState);
        }
    }

    [ArchivePatch(typeof(PlayerChatManager), nameof(PlayerChatManager.DoSendChatMessage))]
    private class PlayerCharManager__DoSendChatMessage__Patch
    {
        private static void Postfix(PlayerChatManager.pChatMessage data)
        {
            if (data.fromPlayer.TryGetPlayer(out var fromPlayer))
            {
                Utils.SafeInvoke(OnReceiveChatMessage, fromPlayer, data.message.data);
            }
        }
    }
}