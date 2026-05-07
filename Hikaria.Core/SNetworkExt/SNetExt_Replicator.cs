using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_Replicator : ISNetExt_Replicator
{
    public ISNetExt_ReplicatorSupplier ReplicatorSupplier
    {
        get => m_supplier;
        set
        {
            m_supplier = value;
            if (m_supplier != null)
            {
                Key = m_supplier.Key;
            }
        }
    }

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

    public bool IsAnonymous => string.IsNullOrWhiteSpace(Key);

    public bool HasValidKeyHash => !string.IsNullOrWhiteSpace(KeyHash) && KeyHash.Length == 32;

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

    public virtual SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.Unspecified;

    public virtual bool OwnedByMaster { get; set; }

    public virtual SNetwork.SNet_Player OwningPlayer { get => m_owningPlayer; set => m_owningPlayer = value; }

    public virtual bool LocallyOwned => OwnedByMaster ? SNetwork.SNet.IsMaster : (OwningPlayer != null && OwningPlayer.IsLocal);

    public void ReceiveBytes(string keyHash, int packetIndex, byte[] bytes)
    {
        if (!m_packetsByKeyHash.TryGetValue(keyHash, out var packets) || packetIndex >= packets.Count)
            return;

        packets[packetIndex].ReceiveBytes(bytes);
    }

    public void AddPacket(SNetExt_ReplicatedPacket packet)
    {
        if (!packet.HasValidKeyHash)
        {
            _logger.Error($"AddPacket, Invalid KeyHash.");
            return;
        }
        if (!m_packetsByKeyHash.TryGetValue(packet.KeyHash, out var packets))
        {
            packets = new();
            m_packetsByKeyHash.Add(packet.KeyHash, packets);
        }
        if (packets.Count > byte.MaxValue)
        {
            _logger.Error($"AddPacket, more than 256 packets share the same KeyHash '{packet.KeyHash}'; rejecting.");
            return;
        }
        packet.Setup(this, (byte)packets.Count);
        packets.Add(packet);
    }

    public SNetExt_ReplicatedPacket<T> CreatePacket<T>(string key, Action<T> receiveAction, Action<T> validateAction = null) where T : struct
    {
        var packet = SNetExt_ReplicatedPacket<T>.Create(key, receiveAction, validateAction);
        AddPacket(packet);
        return packet;
    }

    public SNetExt_ReplicatedPacketBufferBytes CreatePacketBufferBytes(string key, SNetExt_ReplicatedPacketBufferBytes.SpanBufferReceiveDelegate receiveAction)
    {
        var packet = SNetExt_ReplicatedPacketBufferBytes.Create(key, receiveAction);
        AddPacket(packet);
        return packet;
    }

    public SNetExt_ReplicatedPacketBytes CreatePacketBytes(string key, SNetExt_ReplicatedPacketBytes.SpanReceiveDelegate receiveAction)
    {
        var packet = SNetExt_ReplicatedPacketBytes.Create(key, receiveAction);
        AddPacket(packet);
        return packet;
    }

    public abstract void Despawn();

    public Type GetPacketType(string key, int packetIndex)
    {
        if (m_packetsByKeyHash.TryGetValue(key, out var packets) && packetIndex >= 0 && packetIndex < packets.Count)
            return packets[packetIndex].GetType();
        return null;
    }

    private string m_key;
    private string m_keyHash;
    private byte[] m_keyHashBytes;
    private ISNetExt_ReplicatorSupplier m_supplier;
    private SNetwork.SNet_Player m_owningPlayer;
    protected Dictionary<string, List<SNetExt_ReplicatedPacket>> m_packetsByKeyHash = new();

    private static readonly byte[] s_emptyHashBytes = new byte[16];

    private static readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Replicator));
}
