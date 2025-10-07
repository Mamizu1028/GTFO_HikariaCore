namespace Hikaria.Core.SNetworkExt;

public class SNetExt_BroadcastAction<T> : SNetExt_SyncedAction<T> where T : struct
{
    public static SNetExt_BroadcastAction<T> Create(string eventName, Action<SNetwork.SNet_Player, T> incomingAction, Func<SNetwork.SNet_Player, bool> listenerFilter = null, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        var action = new SNetExt_BroadcastAction<T>();
        action.Setup(eventName, incomingAction, null, listenerFilter, channelType);
        return action;
    }

    public void Do(T data)
    {
        m_packet.Send(data, m_listeners);
    }
}
