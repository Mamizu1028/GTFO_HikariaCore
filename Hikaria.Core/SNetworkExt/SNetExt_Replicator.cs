using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_Replicator : ISNetExt_MutableReplicator
{
    public ISNetExt_ReplicatorSupplier ReplicatorSupplier
    {
        get => m_supplier;
        set
        {
            m_supplier = value;
            if (m_supplier != null)
                AssignKeyCore(m_supplier.Key);
        }
    }

    public string Key => m_key;

    public string KeyHash => m_keyHash;

    public ReadOnlySpan<byte> KeyHashBytes => m_keyHashBytes;

    public bool IsAnonymous => string.IsNullOrWhiteSpace(m_key);

    public bool HasValidKeyHash => !string.IsNullOrWhiteSpace(m_keyHash) && m_keyHash.Length == 32;

    public virtual SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.Unspecified;

    public virtual SNetwork.SNet_Player OwningPlayer => m_owningPlayer;

    public virtual bool OwnedByMaster => m_ownedByMaster;

    public abstract bool LocallyOwned { get; }

    void ISNetExt_MutableReplicator.AssignKey(string key) => AssignKeyCore(key);

    void ISNetExt_MutableReplicator.AssignKeyHash(in SNetExt_KeyHash16 keyHash) => AssignKeyHashCore(keyHash);

    void ISNetExt_MutableReplicator.ReceiveBytes(in SNetExt_KeyHash16 packetKeyHash, byte packetIndex, byte[] bytes)
    {
        if (!m_packetsByKeyHash.TryGetValue(packetKeyHash, out var packets) || packetIndex >= packets.Count)
            return;

        packets[packetIndex].ReceiveBytes(bytes);
    }

    protected void AssignKeyCore(string key)
    {
        m_key = key ?? string.Empty;
        if (string.IsNullOrWhiteSpace(m_key))
        {
            AssignKeyHashCore(default);
            return;
        }
        Span<byte> hashBytes = stackalloc byte[16];
        SNetExt_HashUtil.KeyToHashBytes(m_key, hashBytes);
        AssignKeyHashCore(SNetExt_KeyHash16.FromSpan(hashBytes));
    }

    protected void AssignKeyHashCore(in SNetExt_KeyHash16 keyHash)
    {
        if (keyHash.IsEmpty)
        {
            m_keyHash = string.Empty;
            Array.Clear(m_keyHashBytes, 0, 16);
            return;
        }
        m_keyHash = keyHash.ToHex();
        keyHash.WriteTo(m_keyHashBytes);
    }

    protected void SetOwningPlayerInternal(SNetwork.SNet_Player player) => m_owningPlayer = player;

    protected void SetOwnedByMasterInternal(bool value) => m_ownedByMaster = value;

    public void AddPacket(SNetExt_ReplicatedPacket packet)
    {
        if (!packet.HasValidKeyHash)
        {
            _logger.Error($"AddPacket, Invalid KeyHash.");
            return;
        }
        var keyHash16 = SNetExt_KeyHash16.FromHex(packet.KeyHash);
        if (!m_packetsByKeyHash.TryGetValue(keyHash16, out var packets))
        {
            packets = new List<SNetExt_ReplicatedPacket>();
            m_packetsByKeyHash.Add(keyHash16, packets);
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
        var keyHash16 = SNetExt_KeyHash16.FromHex(key);
        if (m_packetsByKeyHash.TryGetValue(keyHash16, out var packets) && packetIndex >= 0 && packetIndex < packets.Count)
            return packets[packetIndex].GetType();
        return null;
    }

    private string m_key = string.Empty;
    private string m_keyHash = string.Empty;
    private readonly byte[] m_keyHashBytes = new byte[16];
    private ISNetExt_ReplicatorSupplier m_supplier;
    private SNetwork.SNet_Player m_owningPlayer;
    private bool m_ownedByMaster;

    protected readonly Dictionary<SNetExt_KeyHash16, List<SNetExt_ReplicatedPacket>> m_packetsByKeyHash = new();

    private static readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Replicator));
}
