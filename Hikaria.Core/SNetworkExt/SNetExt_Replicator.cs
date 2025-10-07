namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replicator : IReplicator
{
    public IReplicatorSupplier ReplicatorSupplier
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
            KeyHash = SNetExt_Replication.ReplicatorKeyToHash(m_key);
            KeyBytes = SNetExt_Replication.ReplicatorKeyToBytes(m_key);
        }
    }

    public string KeyHash { get; private set; }

    public byte[] KeyBytes { get; set; }

    public virtual SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.Unspecified;

    public bool OwnedByMaster { get; set; }

    public SNetwork.SNet_Player OwningPlayer { get => m_owningPlayer; set => m_owningPlayer = value; }

    public virtual bool LocallyOwned => OwnedByMaster ? SNetwork.SNet.IsMaster : (OwningPlayer != null && OwningPlayer.IsLocal);

    public void ReceiveBytes(string key, int packetIndex, byte[] bytes)
    {
        if (!m_packetsByKey.TryGetValue(key, out var packets) || packetIndex >= packets.Count)
            return;

        packets[packetIndex].ReceiveBytes(bytes);
    }

    public void AddPacket(SNetExt_ReplicatedPacket packet)
    {
        if (!m_packetsByKey.TryGetValue(packet.KeyHash, out var packets))
        {
            packets = new();
            m_packetsByKey.Add(packet.KeyHash, packets);
        }
        packet.Setup(this, (ushort)packets.Count);
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

    public SNetExt_ReplicatedPacketBufferBytes CreatePacketBytes(string key, Action<byte[], SNetExt_ReplicatedPacketBufferBytes.BufferData> receiveAction)
    {
        var packet = SNetExt_ReplicatedPacketBufferBytes.Create(key, receiveAction);
        AddPacket(packet);
        return packet;
    }

    public virtual void Despawn()
    {
        throw new NotImplementedException("This does nothing on base class, override this otherwise it will leave grabage in Replication");
    }

    public Type GetPacketType(string key, int packetIndex)
    {
        if (m_packetsByKey.TryGetValue(key, out var packets) && packetIndex >= 0 && packetIndex < packets.Count)
            return packets[packetIndex].GetType();
        return null;
    }

    private string m_key;
    private IReplicatorSupplier m_supplier;
    private SNetwork.SNet_Player m_owningPlayer;
    protected Dictionary<string, List<SNetExt_ReplicatedPacket>> m_packetsByKey = new();
    protected Dictionary<string, string> m_packetKeyToKeyHash = new();
    protected Dictionary<string, string> m_packetKeyHashToKey = new();
}
