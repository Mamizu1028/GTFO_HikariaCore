namespace Hikaria.Core.SNetworkExt;

public class SNetExt_BroadcastAction<T> : SNetExt_SyncedAction<T> where T : struct
{
    public static SNetExt_BroadcastAction<T> Create(string eventName, Action<ulong, T> incomingAction, Func<SNetwork.SNet_Player, bool> listenerFilter = null, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        SNetExt_BroadcastAction<T> snet_BroadcastAction = new SNetExt_BroadcastAction<T>();
        snet_BroadcastAction.Setup(eventName, incomingAction, null, listenerFilter, channelType);
        return snet_BroadcastAction;
    }

    public void Do(T data)
    {
        m_packet.Send(data, m_listeners);
        m_incomingAction(SNetwork.SNet.LocalPlayer.Lookup, data);
    }
}
