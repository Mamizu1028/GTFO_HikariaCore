using GTFO.API;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_ReplicatedPacketBufferBytes : SNetExt_ReplicatedPacket
{
    public delegate void SpanBufferReceiveDelegate(ReadOnlySpan<byte> payload, BufferData bufferData);

    public SNetwork.SNet_ChannelType ChannelType
    {
        get => m_channelType;
        set => m_channelType = value;
    }

    private SpanBufferReceiveDelegate ReceiveAction { get; set; }

    public static SNetExt_ReplicatedPacketBufferBytes Create(string key, SpanBufferReceiveDelegate receiveAction)
    {
        if (receiveAction == null)
            throw new ArgumentNullException(nameof(receiveAction));

        return new SNetExt_ReplicatedPacketBufferBytes
        {
            Key = key,
            ReceiveAction = receiveAction
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBufferDataBytes(BufferData bufferData, Span<byte> destination3)
    {
        if (destination3.Length < 3)
            throw new ArgumentException("need 3 bytes", nameof(destination3));
        destination3[0] = (byte)(bufferData.bufferID & 0xFF);
        destination3[1] = (byte)((bufferData.bufferID >> 8) & 0xFF);
        destination3[2] = bufferData.pass;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BufferData GetBufferData(byte[] bytes)
    {
        ushort bufferID = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(33, 2));
        byte pass = bytes[35];
        return new BufferData(bufferID, pass);
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
        int payloadStart = 33 + BUFFER_DATA_BYTE_SIZE;
        int payloadLength = bytes.Length - payloadStart;
        if (payloadLength < 0) return;
        var bufferData = GetBufferData(bytes);
        ReceiveAction.Invoke(bytes.AsSpan(payloadStart, payloadLength), bufferData);
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
