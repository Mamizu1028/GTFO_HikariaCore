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
            m_key = value;
            if (string.IsNullOrWhiteSpace(m_key))
            {
                m_keyHash = string.Empty;
                m_keyHashBytes = new byte[16];
                return;
            }
            KeyHash = SNetExt_Replication.ReplicatorKeyToHash(m_key);
        }
    }

    public bool IsAnonymous => string.IsNullOrWhiteSpace(Key);

    public bool HasValidKeyHash => !string.IsNullOrWhiteSpace(KeyHash) && KeyHash.Length == 32;

    public string KeyHash
    {
        get => m_keyHash;
        set
        {
            m_keyHash = value;
            KeyHashBytes = SNetExt_Replication.ReplicatorKeyHashToBytes(m_keyHash);
        }
    }

    public byte[] KeyHashBytes { get => m_keyHashBytes; private set => m_keyHashBytes = value; }

    public virtual SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.Unspecified;

    public bool OwnedByMaster { get; set; }

    public SNetwork.SNet_Player OwningPlayer { get => m_owningPlayer; set => m_owningPlayer = value; }

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
        packet.Setup(this, (byte)packets.Count);
        packets.Add(packet);
        m_packetKeyHashToKey[packet.KeyHash] = packet.Key;
        m_packetKeyToKeyHash[packet.Key] = packet.KeyHash;
    }

    public SNetExt_ReplicatedPacket<T> CreatePacket<T>(string key, Action<T> receiveAction, Action<T> validateAction = null) where T : struct
    {
        var packet = SNetExt_ReplicatedPacket<T>.Create(key, receiveAction, validateAction);
        AddPacket(packet);
        return packet;
    }

    public SNetExt_ReplicatedPacketBufferBytes CreatePacketBufferBytes(string key, Action<byte[], SNetExt_ReplicatedPacketBufferBytes.BufferData> receiveAction)
    {
        var packet = SNetExt_ReplicatedPacketBufferBytes.Create(key, receiveAction);
        AddPacket(packet);
        return packet;
    }

    public SNetExt_ReplicatedPacketBytes CreatePacketBytes(string key, Action<byte[]> receiveAction)
    {
        var packet = SNetExt_ReplicatedPacketBytes.Create(key, receiveAction);
        AddPacket(packet);
        return packet;
    }

    public virtual void Despawn()
    {
        throw new NotImplementedException("This does nothing on base class, override this otherwise it will leave grabage in Replication");
    }

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
    protected Dictionary<string, string> m_packetKeyToKeyHash = new();
    protected Dictionary<string, string> m_packetKeyHashToKey = new();

    private static readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Replicator));
}
