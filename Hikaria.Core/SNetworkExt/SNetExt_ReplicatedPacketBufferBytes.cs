using GTFO.API;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_ReplicatedPacketBufferBytes : SNetExt_ReplicatedPacket
{
    private Action<byte[], BufferData> ReceiveAction { get; set; }

    public static SNetExt_ReplicatedPacketBufferBytes Create(string key, Action<byte[], BufferData> receiveAction)
    {
        var packet = new SNetExt_ReplicatedPacketBufferBytes
        {
            Key = key,
            ReceiveAction = receiveAction
        };
        return packet;
    }

    public static byte[] GetBufferDataBytes(BufferData bufferData)
    {
        return new byte[3]
        {
            (byte)(bufferData.bufferID & 0xFF),
            (byte)((bufferData.bufferID >> 8) & 0xFF),
            bufferData.pass
        };
    }

    public static BufferData GetBufferData(byte[] bytes)
    {
        ushort[] array = new ushort[1];
        Buffer.BlockCopy(bytes, 33 + BUFFER_DATA_BYTE_SIZE, array, 0, 2);
        return new BufferData(array[0], bytes[33 + 2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(byte[] data, byte[] bufferDataBytes)
    {
        byte[] bytes = new byte[data.Length + 33 + BUFFER_DATA_BYTE_SIZE];
        Buffer.BlockCopy(bufferDataBytes, 0, bytes, 33, BUFFER_DATA_BYTE_SIZE);
        Buffer.BlockCopy(data, 0, bytes, 33 + BUFFER_DATA_BYTE_SIZE, data.Length);
        InjectIDPacketIndex(this, bytes, Replicator.KeyHashBytes, KeyHashBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, bytes, m_channelType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(byte[] data, byte[] bufferDataBytes, SNetwork.SNet_Player toPlayer)
    {
        byte[] bytes = new byte[data.Length + 33 + BUFFER_DATA_BYTE_SIZE];
        Buffer.BlockCopy(bufferDataBytes, 0, bytes, 33, BUFFER_DATA_BYTE_SIZE);
        Buffer.BlockCopy(data, 0, bytes, 33 + BUFFER_DATA_BYTE_SIZE, data.Length);
        InjectIDPacketIndex(this, bytes, Replicator.KeyHashBytes, KeyHashBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, bytes, toPlayer, m_channelType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(byte[] data, byte[] bufferDataBytes, List<SNetwork.SNet_Player> toPlayers)
    {
        byte[] bytes = new byte[data.Length + 33 + BUFFER_DATA_BYTE_SIZE];
        Buffer.BlockCopy(bufferDataBytes, 0, bytes, 33, BUFFER_DATA_BYTE_SIZE);
        Buffer.BlockCopy(data, 0, bytes, 33 + BUFFER_DATA_BYTE_SIZE, data.Length);
        InjectIDPacketIndex(this, bytes, Replicator.KeyHashBytes, KeyHashBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, bytes, toPlayers, m_channelType);
    }

    public override void ReceiveBytes(byte[] bytes)
    {
        byte[] bufferDataBytes = new byte[bytes.Length - 33 - BUFFER_DATA_BYTE_SIZE];
        Buffer.BlockCopy(bytes, 33 + BUFFER_DATA_BYTE_SIZE, bufferDataBytes, 0, bufferDataBytes.Length);
        ReceiveAction(bufferDataBytes, GetBufferData(bytes));
    }

    public static readonly int BUFFER_DATA_BYTE_SIZE = Marshal.SizeOf<BufferData>();

    private SNetwork.SNet_ChannelType m_channelType = SNetwork.SNet_ChannelType.SessionOrderCritical;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct BufferData
    {
        public BufferData(ushort bufferID, byte pass)
        {
            this.bufferID = bufferID;
            this.pass = pass;
        }

        public ushort bufferID;

        public byte pass;
    }
}
