namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_SyncedAction<T> where T : struct
{
    protected void Setup(string eventName, Action<ulong, T> incomingAction, Action<T> incomingActionValidation = null, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        m_packet = SNetExt_Packet<T>.Create(eventName, incomingAction, incomingActionValidation, false);
        m_channelType = channelType;
        m_incomingAction = incomingAction;
    }

    protected SNetExt_Packet<T> m_packet;

    protected Action<ulong, T> m_incomingAction;

    protected SNetwork.SNet_ChannelType m_channelType;
}
