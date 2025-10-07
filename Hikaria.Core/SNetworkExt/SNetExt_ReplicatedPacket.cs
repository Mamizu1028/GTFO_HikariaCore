using GTFO.API;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_ReplicatedPacket
{
    public IReplicator Replicator { get; set; }

    public ushort Index { get; private set; }

    public string Key
    {
        get => m_key;
        set
        {
            m_key = value;
            KeyHash = PacketKeyToHash(m_key);
            KeyBytes = PacketKeyToBytes(m_key);
        }
    }

    public byte[] KeyBytes { get; private set; }

    public string KeyHash { get; private set; }

    public virtual void Setup(IReplicator replicator, ushort index)
    {
        if (string.IsNullOrWhiteSpace(Key))
            Key = GetType().FullName;
        Replicator = replicator;
        Index = index;
    }

    public virtual void ReceiveBytes(byte[] bytes)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void InjectIDPacketIndex(byte[] bytes)
    {
        using (MD5 md5 = MD5.Create())
        {
            Buffer.BlockCopy(Replicator.KeyBytes, 0, bytes, 0, 16);
            Buffer.BlockCopy(KeyBytes, 0, bytes, 16, 16);
        }

        bytes[32] = (byte)(Index & 0xFF);
        bytes[33] = (byte)((Index >> 8) & 0xFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void InjectIDPacketIndex(SNetExt_ReplicatedPacket packet, byte[] bytes, byte[] replicatorKeyBytes, byte[] packetKeyBytes)
    {
        using (MD5 md5 = MD5.Create())
        {
            Buffer.BlockCopy(replicatorKeyBytes, 0, bytes, 0, 16);
            Buffer.BlockCopy(packetKeyBytes, 0, bytes, 16, 16);
        }

        bytes[32] = (byte)(packet.Index & 0xFF);
        bytes[33] = (byte)((packet.Index >> 8) & 0xFF);
    }

    protected SNetExt_ReplicatedPacket()
    {
    }

    public static byte[] PacketKeyToBytes(string key)
    {
        using MD5 md5 = MD5.Create();
        return md5.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    public static string PacketKeyToHash(string key)
    {
        using MD5 md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
    }

    private string m_key;
}

public class SNetExt_ReplicatedPacket<T> : SNetExt_ReplicatedPacket where T : struct
{
    public override void Setup(IReplicator replicator, ushort index)
    {
        Key = typeof(T).FullName;
        base.Setup(replicator, index);
        SetInternalSize();
    }

    private Action<T> ValidateAction { get; set; }

    private Action<T> ReceiveAction { get; set; }

    public static SNetExt_ReplicatedPacket<T> Create(string key, Action<T> receiveAction, Action<T> validateAction = null)
    {
        if (!s_hasMarshaller)
        {
            s_marshaller = SNetExt_Marshal.GetMarshaler<T>();
            s_hasMarshaller = s_marshaller != null;
            if (!s_hasMarshaller)
                return null;
        }
        return new SNetExt_ReplicatedPacket<T>
        {
            Key = string.IsNullOrWhiteSpace(key) ? typeof(T).FullName : key,
            ReceiveAction = receiveAction,
            ValidateAction = validateAction,
            m_hasValidateAction = validateAction != null
        };
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(T data, SNetwork.SNet_ChannelType type)
    {
        Marshal.StructureToPtr(data, s_marshaller.m_intPtr, true);
        Marshal.Copy(s_marshaller.m_intPtr, m_internalBytes, 34, s_marshaller.Size);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, m_internalBytes, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(T data, SNetwork.SNet_ChannelType type, SNetwork.SNet_Player player)
    {
        Marshal.StructureToPtr(data, s_marshaller.m_intPtr, true);
        Marshal.Copy(s_marshaller.m_intPtr, m_internalBytes, 34, s_marshaller.Size);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, m_internalBytes, player, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(T data, SNetwork.SNet_ChannelType type, List<SNetwork.SNet_Player> players)
    {
        Marshal.StructureToPtr(data, s_marshaller.m_intPtr, true);
        Marshal.Copy(s_marshaller.m_intPtr, m_internalBytes, 34, s_marshaller.Size);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, m_internalBytes, players, type);
    }

    public void CaptureToBuffer(T data, SNetwork.eCapturePass captureDataType)
    {
        s_marshaller.MarshalToBytes(data, m_internalBytes);
        SNetExt.Capture.CaptureToBuffer(m_internalBytes, captureDataType);
    }

    public override void ReceiveBytes(byte[] bytes)
    {
        if (bytes.Length == s_marshaller.SizeWithIDs)
        {
            s_marshaller.MarshalToData(bytes, ref m_data);
            if (m_hasValidateAction && SNetwork.SNet.IsMaster)
            {
                ValidateAction(m_data);
                return;
            }
            ReceiveAction(m_data);
        }
    }

    private void SetInternalSize()
    {
        m_internalBytes = new byte[s_marshaller.SizeWithIDs];
        InjectIDPacketIndex(m_internalBytes);
    }

    private T m_data = new();

    private byte[] m_internalBytes;

    private bool m_hasValidateAction;

    private static bool s_hasMarshaller;

    private static SNetExt_Marshaller<T> s_marshaller;
}
