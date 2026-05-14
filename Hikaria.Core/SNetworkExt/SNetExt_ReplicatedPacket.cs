using GTFO.API;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_ReplicatedPacket
{
    public ISNetExt_Replicator Replicator { get; private set; }

    public byte Index { get; private set; }

    public string Key
    {
        get => m_key;
        set
        {
            m_key = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(m_key))
            {
                m_keyHash = string.Empty;
                m_keyHashBytes = s_emptyHashBytes;
                return;
            }
            Span<byte> hashBytes = stackalloc byte[16];
            SNetExt_HashUtil.KeyToHashBytes(m_key, hashBytes);
            m_keyHash = SNetExt_HashUtil.HashBytesToHex(hashBytes);
            m_keyHashBytes = hashBytes.ToArray();
        }
    }

    public string KeyHash
    {
        get => m_keyHash;
        set
        {
            m_keyHash = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(m_keyHash) || m_keyHash.Length != 32)
            {
                m_keyHashBytes = s_emptyHashBytes;
                return;
            }
            m_keyHashBytes = SNetExt_HashUtil.HashHexToBytes(m_keyHash);
        }
    }

    public byte[] KeyHashBytes { get => m_keyHashBytes; private set => m_keyHashBytes = value; }

    public bool IsAnonymous => string.IsNullOrWhiteSpace(Key);

    public bool HasValidKeyHash => !string.IsNullOrWhiteSpace(KeyHash) && KeyHash.Length == 32;

    public virtual void Setup(ISNetExt_Replicator replicator, byte index)
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
        Replicator.KeyHashBytes.CopyTo(bytes.AsSpan(0, 16));
        Buffer.BlockCopy(KeyHashBytes, 0, bytes, 16, 16);
        bytes[32] = Index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InjectIDPacketIndex(SNetExt_ReplicatedPacket packet, byte[] bytes, ReadOnlySpan<byte> replicatorKeyHashBytes, byte[] packetKeyHashBytes)
    {
        replicatorKeyHashBytes.CopyTo(bytes.AsSpan(0, 16));
        Buffer.BlockCopy(packetKeyHashBytes, 0, bytes, 16, 16);
        bytes[32] = packet.Index;
    }

    protected SNetExt_ReplicatedPacket()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] PacketKeyHashToBytes(string keyHash)
    {
        return SNetExt_HashUtil.HashHexToBytes(keyHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string PacketKeyToHash(string key)
    {
        return SNetExt_HashUtil.KeyToHashHex(key);
    }

    private string m_key;
    private string m_keyHash;
    private byte[] m_keyHashBytes;

    private static readonly byte[] s_emptyHashBytes = new byte[16];
}

public class SNetExt_ReplicatedPacket<T> : SNetExt_ReplicatedPacket where T : struct
{
    public override void Setup(ISNetExt_Replicator replicator, byte index)
    {
        if (string.IsNullOrWhiteSpace(Key))
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
        WritePayload(data);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, m_internalBytes, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(T data, SNetwork.SNet_ChannelType type, SNetwork.SNet_Player player)
    {
        WritePayload(data);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, m_internalBytes, player, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send(T data, SNetwork.SNet_ChannelType type, List<SNetwork.SNet_Player> players)
    {
        WritePayload(data);
        NetworkAPI.InvokeFreeSizedEvent(SNetExt_Replication.NETWORK_EVENT_NAME, m_internalBytes, players, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePayload(T data)
    {
        if (s_marshaller.IsBlittable)
        {
            MemoryMarshal.Write(m_internalBytes.AsSpan(33, s_marshaller.Size), ref data);
            return;
        }
        IntPtr ptr = Marshal.AllocHGlobal(s_marshaller.Size);
        try
        {
            Marshal.StructureToPtr(data, ptr, false);
            Marshal.Copy(ptr, m_internalBytes, 33, s_marshaller.Size);
        }
        finally
        {
            Marshal.DestroyStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
        }
    }

    internal void CaptureToBuffer(T data, SNetExt_CapturePass captureDataType)
    {
        s_marshaller.MarshalToBytes(data, m_internalBytes);
        SNetExt.Capture.CaptureToBuffer(m_internalBytes, captureDataType);
    }

    public override void ReceiveBytes(byte[] bytes)
    {
        if (bytes.Length != s_marshaller.SizeWithIDs)
        {
            SNetExt.Logger.Warning(
                $"ReceiveBytes<{typeof(T).Name}>: size mismatch (expected {s_marshaller.SizeWithIDs}, got {bytes.Length})");
            return;
        }
        s_marshaller.MarshalToData(bytes, ref m_data);
        if (m_hasValidateAction && SNetwork.SNet.IsMaster)
        {
            ValidateAction(m_data);
            return;
        }
        ReceiveAction(m_data);
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
