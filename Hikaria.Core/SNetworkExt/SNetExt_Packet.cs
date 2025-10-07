using GTFO.API;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Packet<T> where T : struct
{
    public string EventName { get; private set; }

    public SNetwork.SNet_ChannelType ChannelType { get; private set; }

    private Action<T> ValidateAction { get; set; }

    private Action<SNetwork.SNet_Player, T> ReceiveAction { get; set; }

    public static SNetExt_Packet<T> Create(string eventName, Action<SNetwork.SNet_Player, T> receiveAction, Action<T> validateAction = null, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        var packet = new SNetExt_Packet<T>
        {
            EventName = eventName,
            ChannelType = channelType,
            ReceiveAction = receiveAction,
            ValidateAction = validateAction,
            m_hasValidateAction = validateAction != null
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
    
    public void Send(T data)
    {
        NetworkAPI.InvokeEvent(EventName, data, ChannelType);
    }

    public void Send(T data, SNetwork.SNet_Player player)
    {
        if (player.IsLocal)
        {
            OnReceiveData(player, data);
            return;
        }
        NetworkAPI.InvokeEvent(EventName, data, player, ChannelType);
    }

    public void Send(T data, List<SNetwork.SNet_Player> players)
    {
        if (players.Count == 0) return;

        var index = players.FindIndex(p => p.IsLocal);
        if (index != -1)
        {
            OnReceiveData(SNetwork.SNet.LocalPlayer, data);
            NetworkAPI.InvokeEvent(EventName, data, players.Where(p => !p.IsLocal).ToList(), ChannelType);
        }
        else
        {
            NetworkAPI.InvokeEvent(EventName, data, players, ChannelType);
        }
    }

    private void OnReceiveData(ulong senderId, T data)
    {
        if (!SNetwork.SNet.TryGetPlayer(senderId, out var sender))
            return;

        OnReceiveData(sender, data);
    }

    private void OnReceiveData(SNetwork.SNet_Player sender, T data)
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

    protected SNetExt_Packet()
    {
    }
}