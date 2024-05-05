using GTFO.API;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_Packet
{
    internal string EventName { get; set; }

    internal bool AllowSendToLocal { get; set; }

    internal SNetwork.SNet_ChannelType ChannelType { get; set; }
}

public class SNetExt_Packet<T> : SNetExt_Packet where T : struct
{
    private Action<T> ValidateAction { get; set; }
    private Action<ulong, T> ReceiveAction { get; set; }

    public static SNetExt_Packet<T> Create(string eventName, Action<ulong, T> receiveAction, Action<T> validateAction = null, bool allowSendToLocal = false, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        var packet = new SNetExt_Packet<T>
        {
            EventName = eventName,
            ChannelType = channelType,
            ReceiveAction = receiveAction,
            ValidateAction = validateAction,
            m_hasValidateAction = validateAction != null,
            AllowSendToLocal = allowSendToLocal
        };
        NetworkAPI.RegisterEvent<T>(eventName, packet.OnReceiveData);
        return packet;
    }

    public void Ask(T data)
    {
        if (SNetwork.SNet.IsMaster)
        {
            ValidateAction(data);
            return;
        }
        if (SNetwork.SNet.HasMaster)
        {
            Send(data, SNetwork.SNet.Master);
        }
    }

    public void Send(T data, SNetwork.SNet_Player player = null)
    {
        if (player == null)
        {
            NetworkAPI.InvokeEvent(EventName, data, ChannelType);
        }
        else
        {
            NetworkAPI.InvokeEvent(EventName, data, player, ChannelType);
            if (AllowSendToLocal && player.IsLocal)
            {
                OnReceiveData(SNetwork.SNet.LocalPlayer.Lookup, data);
            }
        }
    }

    public void Send(T data, params SNetwork.SNet_Player[] players)
    {
        if (players == null || players.Count() == 0) return;

        NetworkAPI.InvokeEvent(EventName, data, players.ToList(), ChannelType);
        if (AllowSendToLocal && players.Any(p => p.IsLocal))
        {
            OnReceiveData(SNetwork.SNet.LocalPlayer.Lookup, data);
        }
    }

    public void Send(T data, List<SNetwork.SNet_Player> players)
    {
        if (players == null || players.Count() == 0) return;

        NetworkAPI.InvokeEvent(EventName, data, players, ChannelType);
        if (AllowSendToLocal && players.Any(p => p.IsLocal))
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