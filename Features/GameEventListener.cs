using GameData;
using Hikaria.Core.Interfaces;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Interfaces;

namespace Hikaria.Core.Features;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
public class GameEventListener : Feature
{
    public override string Name => "Game Event Listener";

    public static GameEventListener Instance { get; private set; }

    public static new IArchiveLogger FeatureLogger { get; set; }

    [ArchivePatch(typeof(SNet_GlobalManager), nameof(SNet_GlobalManager.Setup))]
    private class SNet_GlobalManager__Setup__Patch
    {
        private static void Postfix()
        {
            OnPlayerEvent += OnPlayerEventM;
            SNet_Events.OnPlayerEvent += OnPlayerEvent;
        }
    }

    [ArchivePatch(typeof(GameDataInit), nameof(GameDataInit.Initialize))]
    private class GameDataInit__Initialize__Patch
    {
        private static void Postfix()
        {
            foreach (var Listener in GameDataInitedListeners)
            {
                try
                {
                    Listener.OnGameDataInited();
                }
                catch (Exception ex)
                {
                    Logs.LogException(ex);
                }
            }
            var onGameDataInited = OnGameDataInited;
            if (onGameDataInited != null)
            {
                onGameDataInited();
            }
        }
    }

    [ArchivePatch(typeof(GameStateManager), nameof(GameStateManager.DoChangeState))]
    private class GameStateManager__DoChangeState__Patch
    {
        private static void Prefix(GameStateManager __instance)
        {
            preState = __instance.m_currentStateName;
        }

        private static void Postfix(eGameStateName nextState)
        {
            foreach (var Listener in GameStateChangeListeners)
            {
                try
                {
                    Listener.OnGameStateChanged(preState, nextState);
                }
                catch (Exception ex)
                {
                    Logs.LogException(ex);
                }
            }
            var onGameStateChanged = OnGameStateChanged;
            if (onGameStateChanged != null)
            {
                onGameStateChanged(preState, nextState);
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
                foreach (var Listener in ChatMessageListeners)
                {
                    try
                    {
                        Listener.OnReceiveChatMessage(fromPlayer, data.message.data);
                    }
                    catch (Exception ex)
                    {
                        Logs.LogException(ex);
                    }
                }
                var onReceiveChatMessage = OnReceiveChatMessage;
                if (onReceiveChatMessage != null)
                {
                    onReceiveChatMessage(fromPlayer, data.message.data);
                }
            }
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
                Logs.LogException(ex);
            }
        }
        switch (playerEvent)
        {
            case SNet_PlayerEvent.PlayerLeftSessionHub:
            case SNet_PlayerEvent.PlayerAgentDeSpawned:
                OnSessionMemberChanged(player, SessionMemberEvent.LeftSessionHub);
                break;
            case SNet_PlayerEvent.PlayerAgentSpawned:
                OnSessionMemberChanged(player, SessionMemberEvent.JoinSessionHub);
                break;
        }
    }


    private static void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        FeatureLogger.Notice($"{player.NickName} [{player.Lookup}] {playerEvent}");
        foreach (var Listener in SessionMemberChangeListeners)
        {
            try
            {
                Listener.OnSessionMemberChanged(player, playerEvent);
            }
            catch (Exception ex)
            {
                Logs.LogException(ex);
            }
        }
    }

    public static void RegisterSelfInGameEventListener<T>(T instance)
    {
        Type type = typeof(T);
        if (type.IsInterface || type.IsAbstract)
            return;
        if (typeof(IOnGameStateChanged).IsAssignableFrom(type))
            GameStateChangeListeners.Add((IOnGameStateChanged)instance);
        if (typeof(IPlayerEventListener).IsAssignableFrom(type))
            PlayerEventListeners.Add((IPlayerEventListener)instance);
        if (typeof(IOnReceiveChatMessage).IsAssignableFrom(type))
            ChatMessageListeners.Add((IOnReceiveChatMessage)instance);
        if (typeof(IOnGameDataInited).IsAssignableFrom(type))
            GameDataInitedListeners.Add((IOnGameDataInited)instance);
        if (typeof(IOnSessionMemberChanged).IsAssignableFrom(type))
            SessionMemberChangeListeners.Add((IOnSessionMemberChanged)instance);
    }

    private static eGameStateName preState;

    private static HashSet<IOnGameDataInited> GameDataInitedListeners = new();

    private static HashSet<IOnGameStateChanged> GameStateChangeListeners = new();

    private static HashSet<IOnReceiveChatMessage> ChatMessageListeners = new();

    private static HashSet<IPlayerEventListener> PlayerEventListeners = new();

    private static HashSet<IOnSessionMemberChanged> SessionMemberChangeListeners = new();

    public static event Action OnGameDataInited;

    public static event Action<eGameStateName, eGameStateName> OnGameStateChanged;

    public static event Action<SNet_Player, string> OnReceiveChatMessage;

    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent;
}