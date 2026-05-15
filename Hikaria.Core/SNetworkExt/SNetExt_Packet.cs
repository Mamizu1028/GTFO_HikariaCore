using GTFO.API;
using System.Runtime.CompilerServices;

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
        if (players == null || players.Count == 0) return;

        bool localFound = false;
        int remoteCount = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsLocal) localFound = true;
            else remoteCount++;
        }

        if (remoteCount > 0)
        {
            if (!localFound)
            {
                NetworkAPI.InvokeEvent(EventName, data, players, ChannelType);
            }
            else
            {
                var remoteList = GetRemotePlayerScratchList();
                remoteList.Clear();
                if (remoteList.Capacity < remoteCount)
                    remoteList.Capacity = remoteCount;
                for (int i = 0; i < players.Count; i++)
                {
                    if (!players[i].IsLocal)
                        remoteList.Add(players[i]);
                }
                NetworkAPI.InvokeEvent(EventName, data, remoteList, ChannelType);
            }
        }

        if (localFound)
            OnReceiveData(SNetwork.SNet.LocalPlayer, data);
    }

    [ThreadStatic]
    private static List<SNetwork.SNet_Player> t_remotePlayerScratch;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<SNetwork.SNet_Player> GetRemotePlayerScratchList()
    {
        return t_remotePlayerScratch ??= new List<SNetwork.SNet_Player>(8);
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