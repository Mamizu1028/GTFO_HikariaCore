using GTFO.API;
using System.Runtime.CompilerServices;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_ReplicatedPacketBytes : SNetExt_ReplicatedPacket
{
    private SNetwork.SNet_ChannelType m_channelType = SNetwork.SNet_ChannelType.SessionOrderCritical;

    private Action<byte[]> ReceiveAction { get; set; }

    public static SNetExt_ReplicatedPacketBytes Create(string key, Action<byte[]> receiveAction)
    {
        var packet = new SNetExt_ReplicatedPacketBytes
        {
            Key = key,
            ReceiveAction = receiveAction
        };
        return packet;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(byte[] data)
    {
        byte[] bytes = new byte[data.Length + 33];
        Buffer.BlockCopy(data, 0, bytes, 33, data.Length);
        InjectIDPacketIndex(this, bytes, Replicator.KeyHashBytes, KeyHashBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, bytes, m_channelType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(byte[] data, SNetwork.SNet_Player toPlayer)
    {
        byte[] bytes = new byte[data.Length + 33];
        Buffer.BlockCopy(data, 0, bytes, 33, data.Length);
        InjectIDPacketIndex(this, bytes, Replicator.KeyHashBytes, KeyHashBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, bytes, toPlayer, m_channelType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(byte[] data, List<SNetwork.SNet_Player> toPlayers)
    {
        byte[] bytes = new byte[data.Length + 33];
        Buffer.BlockCopy(data, 0, bytes, 33, data.Length);
        InjectIDPacketIndex(this, bytes, Replicator.KeyHashBytes, KeyHashBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, bytes, toPlayers, m_channelType);
    }

    public override void ReceiveBytes(byte[] bytes)
    {
        byte[] bufferDataBytes = new byte[bytes.Length - 33];
        Buffer.BlockCopy(bytes, 33, bufferDataBytes, 0, bufferDataBytes.Length);
        ReceiveAction(bufferDataBytes);
    }
}
