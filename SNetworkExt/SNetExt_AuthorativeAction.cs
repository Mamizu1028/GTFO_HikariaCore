namespace Hikaria.Core.SNetworkExt;

public class SNetExt_AuthorativeAction<T> : SNetExt_SyncedAction<T> where T : struct
{
    public static SNetExt_AuthorativeAction<T> Create(string eventName, Action<ulong, T> incomingAction, Action<T> incomingActionValidation, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        SNetExt_AuthorativeAction<T> SNetExt_AuthorativeAction = new SNetExt_AuthorativeAction<T>();
        SNetExt_AuthorativeAction.Setup(eventName, incomingAction, incomingActionValidation, channelType);
        SNetExt_AuthorativeAction.m_incomingActionValidation = incomingActionValidation;
        return SNetExt_AuthorativeAction;
    }

    public void Ask(T data)
    {
        if (SNetwork.SNet.IsMaster)
        {
            m_incomingActionValidation(data);
            return;
        }
        if (SNetwork.SNet.HasMaster)
        {
            m_packet.Send(data, SNetwork.SNet.Master);
        }
    }

    public void Do(T data)
    {
        if (SNetwork.SNet.IsMaster)
        {
            m_packet.Send(data);
            m_incomingAction(SNetwork.SNet.LocalPlayer.Lookup, data);
            return;
        }
        if (SNetwork.SNet.HasMaster)
        {
            m_packet.Send(data, SNetwork.SNet.Master);
        }
    }

    private Action<T> m_incomingActionValidation;
}
