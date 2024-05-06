using Hikaria.Core.Interfaces;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_SyncedAction<T> : IOnSessionMemberChanged where T : struct
{
    ~SNetExt_SyncedAction()
    {
        GameEventAPI.UnregisterSelf(this);
    }

    protected void Setup(string eventName, Action<ulong, T> incomingAction, Action<T> incomingActionValidation = null, Func<SNetwork.SNet_Player, bool> listenerFilter = null, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        m_packet = SNetExt_Packet<T>.Create(eventName, incomingAction, incomingActionValidation, false, channelType);
        m_incomingAction = incomingAction;
        m_listenerFilter = listenerFilter;
        m_hasListenerFilter = listenerFilter != null;
        GameEventAPI.RegisterSelf(this);
    }

    public void OnSessionMemberChanged(SNetwork.SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (playerEvent == SessionMemberEvent.JoinSessionHub)
        {
            if (m_hasListenerFilter && !m_listenerFilter(player))
                return;

            m_listeners.RemoveAll(p => p.Lookup == player.Lookup);
            m_listeners.Add(player);
            m_listenersLookup[player.Lookup] = player;

            var onPlayerAddedToListeners = OnPlayerAddedToListeners;
            if (onPlayerAddedToListeners != null)
                onPlayerAddedToListeners(player);
        }
        else if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            if (player.IsLocal)
            {
                var onPlayerRemovedFromListeners = OnPlayerRemovedFromListeners;
                foreach (var listener in m_listeners)
                {
                    m_listeners.RemoveAll(p => p.Lookup == player.Lookup);
                    m_listenersLookup.Remove(player.Lookup);
                    if (onPlayerRemovedFromListeners != null)
                    {
                        onPlayerRemovedFromListeners(listener);
                    }
                }
            }
            else
            {
                m_listeners.RemoveAll(p => p.Lookup == player.Lookup);
                m_listenersLookup.Remove(player.Lookup);

                var onPlayerRemovedFromListeners = OnPlayerRemovedFromListeners;
                if (onPlayerRemovedFromListeners != null)
                    onPlayerRemovedFromListeners(player);
            }
        }
    }

    public void AddPlayerToListeners(SNetwork.SNet_Player player)
    {
        if (m_hasListenerFilter && !m_listenerFilter(player))
            return;

        m_listeners.RemoveAll(p => p.Lookup == player.Lookup);
        m_listeners.Add(player);
        m_listenersLookup[player.Lookup] = player;

        var onPlayerAddedToListeners = OnPlayerAddedToListeners;
        if (onPlayerAddedToListeners != null)
            onPlayerAddedToListeners(player);
    }

    public void RemovePlayerFromListeners(SNetwork.SNet_Player player)
    {
        if (player.IsLocal)
        {
            var onPlayerRemovedFromListeners = OnPlayerRemovedFromListeners;
            foreach (var listener in m_listeners)
            {
                m_listeners.RemoveAll(p => p.Lookup == player.Lookup);
                m_listenersLookup.Remove(player.Lookup);
                if (onPlayerRemovedFromListeners != null)
                {
                    onPlayerRemovedFromListeners(listener);
                }
            }
        }
        else
        {
            m_listeners.RemoveAll(p => p.Lookup == player.Lookup);
            m_listenersLookup.Remove(player.Lookup);

            var onPlayerRemovedFromListeners = OnPlayerRemovedFromListeners;
            if (onPlayerRemovedFromListeners != null)
                onPlayerRemovedFromListeners(player);
        }
    }

    public event Action<SNetwork.SNet_Player> OnPlayerAddedToListeners;

    public event Action<SNetwork.SNet_Player> OnPlayerRemovedFromListeners;

    protected SNetExt_Packet<T> m_packet;

    protected Action<ulong, T> m_incomingAction;

    protected Func<SNetwork.SNet_Player, bool> m_listenerFilter;

    protected bool m_hasListenerFilter;

    protected List<SNetwork.SNet_Player> m_listeners;

    protected Dictionary<ulong, SNetwork.SNet_Player> m_listenersLookup;

    public IEnumerable<SNetwork.SNet_Player> Listeners => m_listeners;

    public IReadOnlyDictionary<ulong, SNetwork.SNet_Player> ListenersLookup => m_listenersLookup;
}
