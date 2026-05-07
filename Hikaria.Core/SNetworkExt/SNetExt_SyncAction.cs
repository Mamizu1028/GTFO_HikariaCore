using TheArchive.Utilities;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_SyncedAction<T> where T : struct
{
    protected SNetExt_SyncedAction() { }

    ~SNetExt_SyncedAction()
    {
        SNetEventAPI.OnSessionMemberChanged -= OnSessionMemberChanged;
        CoreAPI.OnPlayerModsSynced -= OnPlayerModsSynced;
        m_listeners.Clear();
        m_listenersLookup.Clear();
    }

    protected void Setup(string eventName, Action<SNetwork.SNet_Player, T> incomingAction, Action<T> incomingActionValidation = null, Func<SNetwork.SNet_Player, bool> listenerFilter = null, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        m_packet = SNetExt_Packet<T>.Create(eventName, incomingAction, incomingActionValidation, channelType);
        m_listenerFilter = listenerFilter;
        m_hasListenerFilter = listenerFilter != null;
        SNetEventAPI.OnSessionMemberChanged += OnSessionMemberChanged;
        CoreAPI.OnPlayerModsSynced += OnPlayerModsSynced;
    }

    public void SyncToPlayer(SNetwork.SNet_Player player, T data)
    {
        m_packet.Send(data, player);
    }

    public void SyncToPlayer(SNetwork.SNet_Player player, params T[] datas)
    {
        foreach (var data in datas)
        {
            SyncToPlayer(player, data);
        }
    }

    public void SyncToPlayer(SNetwork.SNet_Player player, IEnumerable<T> datas)
    {
        foreach (var data in datas)
        {
            SyncToPlayer(player, data);
        }
    }

    private void OnPlayerModsSynced(SNetwork.SNet_Player player, IEnumerable<pModInfo> mods)
    {
        if (!m_hasListenerFilter || !m_listenerFilter(player))
            return;
        if (player.IsInSessionHub)
        {
            Internal_AddPlayerToListeners(player);
        }
    }

    private void OnSessionMemberChanged(SNetwork.SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (playerEvent == SessionMemberEvent.JoinSessionHub)
        {
            if (m_hasListenerFilter && !m_listenerFilter(player))
                return;

            Internal_AddPlayerToListeners(player);
        }
        else if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            Internal_RemovePlayerFromListeners(player);
        }
    }

    private void Internal_AddPlayerToListeners(SNetwork.SNet_Player player)
    {
        if (m_listenersLookup.ContainsKey(player.Lookup))
        {
            for (int i = 0; i < m_listeners.Count; i++)
            {
                if (m_listeners[i].Lookup == player.Lookup)
                {
                    m_listeners[i] = player;
                    break;
                }
            }
            m_listenersLookup[player.Lookup] = player;
            return;
        }
        m_listeners.Add(player);
        m_listenersLookup[player.Lookup] = player;
        Utils.SafeInvoke(OnPlayerAddedToListeners, player);
    }

    private void Internal_RemovePlayerFromListeners(SNetwork.SNet_Player player)
    {
        if (player.IsLocal)
        {
            var oldListeners = m_listeners;
            m_listeners = new List<SNetwork.SNet_Player>();
            m_listenersLookup.Clear();
            for (int i = 0; i < oldListeners.Count; i++)
            {
                Utils.SafeInvoke(OnPlayerRemovedFromListeners, oldListeners[i]);
            }
            return;
        }

        int removed = m_listeners.RemoveAll(p => p.Lookup == player.Lookup);
        m_listenersLookup.Remove(player.Lookup);
        if (removed > 0)
        {
            Utils.SafeInvoke(OnPlayerRemovedFromListeners, player);
        }
    }

    public void AddPlayerToListeners(SNetwork.SNet_Player player)
    {
        if (m_hasListenerFilter && !m_listenerFilter(player))
            return;

        Internal_AddPlayerToListeners(player);
    }

    public void RemovePlayerFromListeners(SNetwork.SNet_Player player)
    {
        Internal_RemovePlayerFromListeners(player);
    }

    public bool IsListener(SNetwork.SNet_Player player)
    {
        if (player == null) return false;
        return IsListener(player.Lookup);
    }

    public bool IsListener(ulong lookup)
    {
        return m_listenersLookup.ContainsKey(lookup);
    }

    public event Action<SNetwork.SNet_Player> OnPlayerAddedToListeners;

    public event Action<SNetwork.SNet_Player> OnPlayerRemovedFromListeners;

    protected SNetExt_Packet<T> m_packet;
    protected Func<SNetwork.SNet_Player, bool> m_listenerFilter;
    protected bool m_hasListenerFilter;
    protected List<SNetwork.SNet_Player> m_listeners = new();
    protected Dictionary<ulong, SNetwork.SNet_Player> m_listenersLookup = new();
    public IEnumerable<SNetwork.SNet_Player> Listeners => m_listeners;
    public IReadOnlyDictionary<ulong, SNetwork.SNet_Player> ListenersLookup => m_listenersLookup;
}
