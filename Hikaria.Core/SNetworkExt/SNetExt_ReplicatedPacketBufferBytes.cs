using GTFO.API;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_ReplicatedPacketBufferBytes : SNetExt_ReplicatedPacket
{
    public SNetwork.SNet_ChannelType ChannelType { get; private set; }

    private Action<byte[], BufferData> ReceiveAction { get; set; }

    public static SNetExt_ReplicatedPacketBufferBytes Create(string key, Action<byte[], BufferData> receiveAction, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.SessionOrderCritical)
    {
        var packet = new SNetExt_ReplicatedPacketBufferBytes
        {
            Key = key,
            ChannelType = channelType,
            ReceiveAction = receiveAction
        };
        return packet;
    }

    public static byte[] GetBufferDataBytes(BufferData bufferData)
    {
        Array array = new ushort[] { bufferData.bufferID };
        byte[] array2 = new byte[3];
        Buffer.BlockCopy(array, 0, array2, 0, 2);
        array2[2] = bufferData.pass;
        return array2;
    }

    public static BufferData GetBufferData(byte[] bytes)
    {
        ushort[] array = new ushort[1];
        Buffer.BlockCopy(bytes, 3, array, 0, 2);
        return new BufferData(array[0], bytes[5]);
    }

    public void Send(byte[] data, byte[] bufferDataBytes)
    {
        byte[] array = new byte[data.Length + 3 + 3];
        Buffer.BlockCopy(bufferDataBytes, 0, array, 3, 3);
        Buffer.BlockCopy(data, 0, array, 6, data.Length);
        //InjectIDPacketIndex(this, array, base.Replicator.KeyBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, array, SNetwork.SNet_ChannelType.SessionOrderCritical);
    }

    public void Send(byte[] data, byte[] bufferDataBytes, SNetwork.SNet_Player toPlayer)
    {
        byte[] array = new byte[data.Length + 3 + 3];
        Buffer.BlockCopy(bufferDataBytes, 0, array, 3, 3);
        Buffer.BlockCopy(data, 0, array, 6, data.Length);
        //InjectIDPacketIndex(this, array, base.Replicator.KeyBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, array, SNetwork.SNet_ChannelType.SessionOrderCritical);
    }

    public void Send(byte[] data, byte[] bufferDataBytes, List<SNetwork.SNet_Player> toPlayers)
    {
        byte[] array = new byte[data.Length + 3 + 3];
        Buffer.BlockCopy(bufferDataBytes, 0, array, 3, 3);
        Buffer.BlockCopy(data, 0, array, 6, data.Length);
        //InjectIDPacketIndex(this, array, base.Replicator.KeyBytes);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, array, SNetwork.SNet_ChannelType.SessionOrderCritical);
    }

    public override void ReceiveBytes(byte[] bytes)
    {
        byte[] array = new byte[bytes.Length - 6];
        Buffer.BlockCopy(bytes, 6, array, 0, array.Length);
        ReceiveAction(array, GetBufferData(bytes));
    }

    public static readonly int BUFFER_DATA_BYTE_SIZE = Marshal.SizeOf<BufferData>();

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
