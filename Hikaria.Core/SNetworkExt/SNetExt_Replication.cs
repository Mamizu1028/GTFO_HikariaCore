using GTFO.API;
using Il2CppInterop.Runtime.Attributes;
using System.Runtime.CompilerServices;
using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replication : MonoBehaviour, ISNetExt_Manager
{
    public const string NETWORK_EVENT_NAME = nameof(SNetExt_Replication);

    private static readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Replication));

    [HideFromIl2Cpp]
    public void Setup()
    {
        NetworkAPI.RegisterFreeSizedEvent(NETWORK_EVENT_NAME, ReceiveBytes);
    }

    [HideFromIl2Cpp]
    public void SetupReplication() { }

    [HideFromIl2Cpp]
    internal void OnRecall()
    {
    }

    [HideFromIl2Cpp]
    internal void OnPostRecall()
    {
    }

    [HideFromIl2Cpp]
    public void OnResetSession()
    {
        CleanupReplicators();
    }

    [HideFromIl2Cpp]
    public void OnValidateMasterData()
    {
        if (m_replicationManagers != null)
        {
            for (int i = 0; i < m_replicationManagers.Count; i++)
            {
                m_replicationManagers[i].OnValidateMasterData();
            }
        }
    }

    [HideFromIl2Cpp]
    public void CleanupReplicators()
    {
        SNetExt.PrefabReplication.OnClearAllSpawnedLocal();
        for (int i = m_assignedReplicators.Count - 1; i > -1; i--)
        {
            var replicator = m_assignedReplicators[i];
            if (replicator != null)
            {
                if (replicator.Type == SNetExt_ReplicatorType.Dynamic)
                {
                    replicator.Despawn();
                }
                else if (replicator.Type == SNetExt_ReplicatorType.SelfManaged)
                {
                    replicator.Key = string.Empty;
                    m_assignedReplicators.RemoveAt(i);
                }
                else if (replicator.Type == SNetExt_ReplicatorType.VanillaWrapper)
                {
                    var wrapper = replicator as SNetExt_Replicator_VanillaWrapper;
                    if (wrapper.VanillaType == SNetwork.SNet_ReplicatorType.Dynamic)
                    {
                        wrapper.Despawn();
                    }
                    else if (wrapper.VanillaType == SNetwork.SNet_ReplicatorType.SelfManaged)
                    {
                        DeallocateVanillaWrapper(wrapper);
                    }
                }
            }
            else
            {
                m_assignedReplicators.RemoveAt(i);
            }
        }
    }

    [HideFromIl2Cpp]
    public List<SNetExt_ReplicationManager> ReplicationManagers => m_replicationManagers;

    [HideFromIl2Cpp]
    public void RegisterReplicationManager(SNetExt_ReplicationManager manager)
    {
        m_replicationManagers.Add(manager);
    }

    [HideFromIl2Cpp]
    public void UnregisterReplicationManager(SNetExt_ReplicationManager manager)
    {
        m_replicationManagers.Remove(manager);
    }

    [HideFromIl2Cpp]
    public bool TryGetLastSender(out SNetwork.SNet_Player sender, bool allowCreate = false)
    {
        return SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out sender, allowCreate);
    }

    [HideFromIl2Cpp]
    public bool IsLastSenderMaster => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out var sender, false) && sender.IsMaster;

    [HideFromIl2Cpp]
    public bool IsLastSenderInSessionHub => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out var sender, false) && sender.IsInSessionHub;

    [HideFromIl2Cpp]
    public bool IsLastSenderInSlot => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out var sender, false) && sender.IsInSessionHub && sender.IsInSlot;

    [HideFromIl2Cpp]
    internal void RecallBytes(byte[] bytes)
    {
        LastSenderID = 0UL;
        if (TryGetReplicator(bytes, out m_tempReplicator, out m_tempPacketKeyHash, out m_tempPacketIndex))
            m_tempReplicator.ReceiveBytes(m_tempPacketKeyHash, m_tempPacketIndex, bytes);
    }

    [HideFromIl2Cpp]
    private void ReceiveBytes(ulong senderId, byte[] bytes)
    {
        LastSenderID = senderId;
        if (bytes.Length < 33)
            return;
        if (TryGetReplicator(bytes, out m_tempReplicator, out m_tempPacketKeyHash, out m_tempPacketIndex))
            m_tempReplicator.ReceiveBytes(m_tempPacketKeyHash, m_tempPacketIndex, bytes);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicatorByKeyHash(string keyHash, out ISNetExt_Replicator replicator)
    {
        var k = SNetExt_KeyHash16.FromHex(keyHash);
        if (k.IsEmpty || !s_replicators.TryGetValue(k, out replicator))
        {
            replicator = null;
            return false;
        }
        return replicator != null;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetReplicatorByKeyHash16(in SNetExt_KeyHash16 keyHash, out ISNetExt_Replicator replicator)
    {
        if (!s_replicators.TryGetValue(keyHash, out replicator))
            return false;
        return replicator != null;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicationSupplierByKeyHash<A>(string keyHash, out A supplier) where A : class
    {
        if (TryGetReplicatorByKeyHash(keyHash, out var replicator) && replicator.ReplicatorSupplier != null)
        {
            supplier = replicator.ReplicatorSupplier as A;
            if (supplier != null)
                return true;
        }
        supplier = default;
        return false;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicator(pReplicationData data, out ISNetExt_Replicator replicator)
    {
        return TryGetReplicatorByKeyHash(data.ReplicatorKeyHash, out replicator);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetReplicator(byte[] bytes, out ISNetExt_Replicator replicator, out string packetKeyHash, out byte packetIndex)
    {
        var replicatorKey = SNetExt_KeyHash16.FromSpan(bytes.AsSpan(0, 16));
        if (!s_replicators.TryGetValue(replicatorKey, out replicator))
        {
            packetKeyHash = string.Empty;
            packetIndex = byte.MaxValue;
            return false;
        }

        packetKeyHash = Convert.ToHexString(bytes, 16, 16);
        packetIndex = bytes[32];
        return replicator != null;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReplicatorKeyHashToBytes(string keyHash)
    {
        return SNetExt_HashUtil.HashHexToBytes(keyHash);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplicatorKeyToHash(string key)
    {
        return SNetExt_HashUtil.KeyToHashHex(key);
    }

    [HideFromIl2Cpp]
    private void AddToAssingedList(ISNetExt_Replicator replicator)
    {
        if (!m_assignedReplicators.Contains(replicator))
        {
            m_assignedReplicators.Add(replicator);
        }
    }

    [HideFromIl2Cpp]
    private void RemoveFromAssignedList(ISNetExt_Replicator replicator)
    {
        m_assignedReplicators.Remove(replicator);
    }

    [HideFromIl2Cpp]
    private void ClearReplicatorKey(string keyHash, ISNetExt_Replicator newReplicator)
    {
        var k = SNetExt_KeyHash16.FromHex(keyHash);
        if (k.IsEmpty || !s_replicators.TryGetValue(k, out var replicator))
            return;
        if (replicator != null)
        {
            if (replicator == newReplicator)
            {
                _logger.Error("ClearReplicatorKey, This happened!");
                return;
            }

            RemoveFromAssignedList(replicator);
            replicator.Despawn();
        }
    }

    [HideFromIl2Cpp]
    public static void AllocateReplicator(ISNetExt_Replicator replicator)
    {
        if (!replicator.HasValidKeyHash)
        {
            _logger.Error($"AllocateReplicator, Invalid KeyHash. Hash: '{replicator.KeyHash}'");
            return;
        }
        var k = SNetExt_KeyHash16.FromHex(replicator.KeyHash);
        if (s_replicators.TryGetValue(k, out var existReplicator))
        {
            _logger.Error($"AllocateReplicator, keyHash Conflict. Exist Key: '{existReplicator.Key}', New Key: '{replicator.Key}', Key Hash: '{replicator.KeyHash}'");
            return;
        }
        s_replicators[k] = replicator;
        s_replicatorKeyToKeyHash[replicator.Key] = replicator.KeyHash;
        s_replicatorKeyHashToKey[replicator.KeyHash] = replicator.Key;
        SNetExt.Replication.AddToAssingedList(replicator);
    }

    [HideFromIl2Cpp]
    public static void AssignReplicatorKey(ISNetExt_Replicator replicator, string keyHash, bool isRecall = false)
    {
        SNetExt.Replication.ClearReplicatorKey(replicator.KeyHash, replicator);

        replicator.Key = string.Empty;
        replicator.KeyHash = keyHash;

        if (!replicator.HasValidKeyHash)
        {
            _logger.Error($"AssignReplicatorKey, Invalid KeyHash. Hash: '{replicator.KeyHash}'");
            return;
        }
        var k = SNetExt_KeyHash16.FromHex(replicator.KeyHash);
        s_replicators[k] = replicator;
        if (!replicator.IsAnonymous)
        {
            s_replicatorKeyToKeyHash[replicator.Key] = replicator.KeyHash;
            s_replicatorKeyHashToKey[replicator.KeyHash] = replicator.Key;
        }
        SNetExt.Replication.AddToAssingedList(replicator);
    }

    [HideFromIl2Cpp]
    public static void DeallocateReplicator(ISNetExt_Replicator replicator)
    {
        var k = SNetExt_KeyHash16.FromHex(replicator.KeyHash);
        if (k.IsEmpty || !s_replicators.TryGetValue(k, out var existReplicator))
            return;
        if (existReplicator == replicator)
        {
            s_replicators.Remove(k);
            if (!replicator.IsAnonymous)
            {
                s_replicatorKeyToKeyHash.Remove(replicator.Key);
                s_replicatorKeyHashToKey.Remove(replicator.KeyHash);
            }
        }
        replicator.Key = string.Empty;
        SNetExt.Replication.RemoveFromAssignedList(replicator);
    }

    [HideFromIl2Cpp]
    public static ISNetExt_Replicator AddManagerReplicator(string key)
    {
        var manager = new SNetExt_Replicator_Manager
        {
            Key = key
        };
        AllocateReplicator(manager);
        return manager;
    }

    [HideFromIl2Cpp]
    public static ISNetExt_Replicator AddManagerReplicator(ISNetExt_ReplicatorSupplier supplier)
    {
        var manager = new SNetExt_Replicator_Manager
        {
            ReplicatorSupplier = supplier
        };
        AllocateReplicator(manager);
        return manager;
    }

    [HideFromIl2Cpp]
    public static ISNetExt_Replicator AddSelfManagedReplicator(string key)
    {
        var replicator = new SNetExt_Replicator_SelfManaged
        {
            Key = key
        };
        AllocateReplicator(replicator);
        return replicator;
    }

    [HideFromIl2Cpp]
    public static ISNetExt_Replicator AddSelfManagedReplicator(ISNetExt_ReplicatorSupplier supplier)
    {
        var replicator = new SNetExt_Replicator_SelfManaged
        {
            ReplicatorSupplier = supplier
        };
        AllocateReplicator(replicator);
        return replicator;
    }

    [HideFromIl2Cpp]
    internal static bool TryBindVanillaWrapper(SNetwork.IReplicator vanilla)
    {
        if (vanilla == null || vanilla.Key == 0)
        {
            return false;
        }
        if (s_byVanillaPointer.TryGetValue(vanilla.Pointer, out var wrapper) && wrapper != null && wrapper.IsAlive)
        {
            return true;
        }
        wrapper = BindVanillaInternal(vanilla);
        return wrapper != null;
    }

    [HideFromIl2Cpp]
    private static SNetExt_Replicator_VanillaWrapper BindVanillaInternal(SNetwork.IReplicator vanilla)
    {
        var wrapper = new SNetExt_Replicator_VanillaWrapper(vanilla);
        if (!wrapper.HasValidKeyHash)
        {
            _logger.Error($"BindVanillaInternal: invalid keyhash for vanilla.Key={vanilla.Key}");
            return null;
        }
        AllocateReplicator(wrapper);
        s_byVanillaPointer[vanilla.Pointer] = wrapper;
        return wrapper;
    }

    [HideFromIl2Cpp]
    internal static bool TryGetVanillaWrapper(SNetwork.IReplicator vanilla, out SNetExt_Replicator_VanillaWrapper wrapper)
    {
        return s_byVanillaPointer.TryGetValue(vanilla.Pointer, out wrapper) && wrapper != null;
    }

    [HideFromIl2Cpp]
    internal static void DeallocateVanillaWrapper(SNetExt_Replicator_VanillaWrapper wrapper)
    {
        DeallocateReplicator(wrapper);
        if (!s_byVanillaPointer.Remove(wrapper.Vanilla.Pointer))
        {
            _logger.Error($"DeallocateVanillaWrapper: Wrapper doesn't exists for Vanilla.Pointer={wrapper.Vanilla.Pointer}");
        }
    }

    private static readonly Dictionary<SNetExt_KeyHash16, ISNetExt_Replicator> s_replicators = new(64);
    private static readonly Dictionary<string, string> s_replicatorKeyToKeyHash = new(64);
    private static readonly Dictionary<string, string> s_replicatorKeyHashToKey = new(64);
    private static readonly Dictionary<IntPtr, SNetExt_Replicator_VanillaWrapper> s_byVanillaPointer = new(64);

    private readonly List<ISNetExt_Replicator> m_assignedReplicators = new();
    private readonly List<SNetExt_ReplicationManager> m_replicationManagers = new();
    private ISNetExt_Replicator m_tempReplicator;
    private string m_tempPacketKeyHash;
    private byte m_tempPacketIndex;

    [HideFromIl2Cpp]
    public ulong LastSenderID { get; private set; }
}
