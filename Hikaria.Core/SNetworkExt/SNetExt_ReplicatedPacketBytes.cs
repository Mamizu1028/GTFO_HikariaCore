using GTFO.API;
using System.Runtime.CompilerServices;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_ReplicatedPacketBytes : SNetExt_ReplicatedPacket
{
    public delegate void SpanReceiveDelegate(ReadOnlySpan<byte> payload);

    public SNetwork.SNet_ChannelType ChannelType
    {
        get => m_channelType;
        set => m_channelType = value;
    }

    private SNetwork.SNet_ChannelType m_channelType = SNetwork.SNet_ChannelType.SessionOrderCritical;

    private SpanReceiveDelegate ReceiveAction { get; set; }

    public static SNetExt_ReplicatedPacketBytes Create(string key, SpanReceiveDelegate receiveAction)
    {
        return new SNetExt_ReplicatedPacketBytes
        {
            Key = key,
            ReceiveAction = receiveAction
        };
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
        int payloadLength = bytes.Length - 33;
        if (payloadLength < 0) return;
        ReceiveAction?.Invoke(bytes.AsSpan(33, payloadLength));
    }
}
