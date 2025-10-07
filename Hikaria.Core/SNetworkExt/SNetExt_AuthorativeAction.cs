namespace Hikaria.Core.SNetworkExt;

public class SNetExt_AuthorativeAction<T> : SNetExt_SyncedAction<T> where T : struct
{
    protected SNetExt_AuthorativeAction()
    {
    }

    public static SNetExt_AuthorativeAction<T> Create(string eventName, Action<SNetwork.SNet_Player, T> incomingAction, Action<T> incomingActionValidation, Func<SNetwork.SNet_Player, bool> listenerFilter = null,SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        var action = new SNetExt_AuthorativeAction<T>();
        action.Setup(eventName, incomingAction, incomingActionValidation, listenerFilter, channelType);
        action.m_incomingActionValidation = incomingActionValidation;
        return action;
    }

    public void Ask(T data)
    {
        if (SNetwork.SNet.IsMaster)
        {
            m_incomingActionValidation(data);
            return;
        }
        if (SNetwork.SNet.HasMaster && m_listenersLookup.ContainsKey(SNetwork.SNet.Master.Lookup))
        {
            m_packet.Send(data, SNetwork.SNet.Master);
        }
    }

    public void Do(T data)
    {
        if (SNetwork.SNet.IsMaster)
        {
            m_packet.Send(data, m_listeners);
            return;
        }
        if (SNetwork.SNet.HasMaster && m_listenersLookup.ContainsKey(SNetwork.SNet.Master.Lookup))
        {
            m_packet.Send(data, SNetwork.SNet.Master);
        }
    }

    private Action<T> m_incomingActionValidation;
}
