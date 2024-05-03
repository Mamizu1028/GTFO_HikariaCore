using GTFO.API;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Packet
{
    internal string EventName { get; set; }
}

public class SNetExt_Packet<T> : SNetExt_Packet where T : struct
{
    private Action<T> ValidateAction { get; set; }
    private Action<ulong, T> ReceiveAction { get; set; }

    public static SNetExt_Packet<T> Create(string eventName, Action<ulong, T> receiveAction, Action<T> validateAction = null)
    {
        var packet = new SNetExt_Packet<T>
        {
            EventName = eventName,
            ReceiveAction = receiveAction,
            ValidateAction = validateAction,
            m_hasValidateAction = validateAction != null
        };
        NetworkAPI.RegisterEvent<T>(eventName, packet.OnReceiveData);
        return packet;
    }

    public void Ask(T data, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.SessionOrderCritical)
    {
        if (SNetwork.SNet.IsMaster)
        {
            ValidateAction(data);
            return;
        }
        if (SNetwork.SNet.HasMaster)
        {
            Send(data, channelType, SNetwork.SNet.Master);
        }
    }

    public void Send(T data, SNetwork.SNet_ChannelType type, SNetwork.SNet_Player player = null)
    {
        if (player == null)
        {
            NetworkAPI.InvokeEvent(EventName, data, type);
        }
        else
        {
            NetworkAPI.InvokeEvent(EventName, data, player, type);
        }
        OnReceiveData(SNetwork.SNet.LocalPlayer.Lookup, data);
    }

    public void Send(T data, SNetwork.SNet_ChannelType type, params SNetwork.SNet_Player[] players)
    {
        if (players == null || players.Count() == 0) return;

        NetworkAPI.InvokeEvent(EventName, data, players.ToList(), type);
        if (players.Any(p => p.IsLocal))
        {
            OnReceiveData(SNetwork.SNet.LocalPlayer.Lookup, data);
        }
    }

    public void OnReceiveData(ulong sender, T data)
    {
        m_data = data;
        if (m_hasValidateAction && SNetwork.SNet.IsMaster)
        {
            ValidateAction(m_data);
            return;
        }
        ReceiveAction(sender, m_data);
    }

    private T m_data = new();

    private bool m_hasValidateAction;
}