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

    private static readonly SNetExt_ReplicatorRegistry s_registry = new();

    internal static SNetExt_ReplicatorRegistry Registry => s_registry;

    [HideFromIl2Cpp]
    public void Setup()
    {
        NetworkAPI.RegisterFreeSizedEvent(NETWORK_EVENT_NAME, ReceiveBytes);
    }

    [HideFromIl2Cpp]
    public void SetupReplication() { }

    [HideFromIl2Cpp]
    internal void OnRecall() => RecallStarted?.Invoke();

    [HideFromIl2Cpp]
    internal void OnPostRecall() => RecallFinished?.Invoke();

    public static event Action RecallStarted;
    public static event Action RecallFinished;

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
                m_replicationManagers[i].OnValidateMasterData();
        }
    }

    [HideFromIl2Cpp]
    internal void CleanupReplicators()
    {
        SNetExt.PrefabReplication.OnClearAllSpawnedLocal();
        var assigned = s_registry.Assigned;
        for (int i = assigned.Count - 1; i >= 0; i--)
        {
            var replicator = assigned[i];
            if (replicator == null) continue;
            switch (replicator.Type)
            {
                case SNetExt_ReplicatorType.Dynamic:
                    replicator.Despawn();
                    break;
                case SNetExt_ReplicatorType.SelfManaged:
                    if (replicator is ISNetExt_MutableReplicator mut)
                        mut.AssignKey(string.Empty);
                    s_registry.Unregister(replicator);
                    break;
                case SNetExt_ReplicatorType.VanillaWrapper:
                    var wrapper = replicator as SNetExt_Replicator_VanillaWrapper;
                    if (wrapper.VanillaType == SNetwork.SNet_ReplicatorType.Dynamic)
                        wrapper.Despawn();
                    else if (wrapper.VanillaType == SNetwork.SNet_ReplicatorType.SelfManaged)
                        DeallocateVanillaWrapper(wrapper);
                    break;
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
        => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out sender, allowCreate);

    [HideFromIl2Cpp]
    internal SNetwork.SNet_Player ResolveLastSender()
        => SNetwork.SNet.Core.TryGetPlayer(LastSenderID, out var p, false) ? p : null;

    [HideFromIl2Cpp]
    public bool IsLastSenderMaster
    {
        get
        {
            var p = ResolveLastSender();
            return p != null && p.IsMaster;
        }
    }

    [HideFromIl2Cpp]
    public bool IsLastSenderInSessionHub
    {
        get
        {
            var p = ResolveLastSender();
            return p != null && p.IsInSessionHub;
        }
    }

    [HideFromIl2Cpp]
    public bool IsLastSenderInSlot
    {
        get
        {
            var p = ResolveLastSender();
            return p != null && p.IsInSessionHub && p.IsInSlot;
        }
    }

    [HideFromIl2Cpp]
    internal void RecallBytes(byte[] bytes)
    {
        LastSenderID = 0UL;
        if (TryGetReplicator(bytes, out var replicator, out var packetKeyHash, out var packetIndex))
            ((ISNetExt_MutableReplicator)replicator).ReceiveBytes(packetKeyHash, packetIndex, bytes);
    }

    [HideFromIl2Cpp]
    private void ReceiveBytes(ulong senderId, byte[] bytes)
    {
        LastSenderID = senderId;
        if (bytes.Length < 33) return;
        if (TryGetReplicator(bytes, out var replicator, out var packetKeyHash, out var packetIndex))
            ((ISNetExt_MutableReplicator)replicator).ReceiveBytes(packetKeyHash, packetIndex, bytes);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicatorByKeyHash(string keyHash, out ISNetExt_Replicator replicator)
    {
        var k = SNetExt_KeyHash16.FromHex(keyHash);
        if (k.IsEmpty)
        {
            replicator = null;
            return false;
        }
        return s_registry.TryGet(k, out replicator);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetReplicatorByKeyHash16(in SNetExt_KeyHash16 keyHash, out ISNetExt_Replicator replicator)
    {
        return s_registry.TryGet(keyHash, out replicator);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicationSupplierByKeyHash<A>(string keyHash, out A supplier) where A : class
    {
        if (TryGetReplicatorByKeyHash(keyHash, out var replicator) && replicator.ReplicatorSupplier != null)
        {
            supplier = replicator.ReplicatorSupplier as A;
            if (supplier != null) return true;
        }
        supplier = default;
        return false;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetReplicator(pReplicationData data, out ISNetExt_Replicator replicator)
    {
        return s_registry.TryGet(data.ReplicatorKeyHash, out replicator);
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetReplicator(byte[] bytes, out ISNetExt_Replicator replicator, out SNetExt_KeyHash16 packetKeyHash, out byte packetIndex)
    {
        var replicatorKey = SNetExt_KeyHash16.FromSpan(bytes.AsSpan(0, 16));
        if (!s_registry.TryGet(replicatorKey, out replicator))
        {
            packetKeyHash = default;
            packetIndex = byte.MaxValue;
            return false;
        }
        packetKeyHash = SNetExt_KeyHash16.FromSpan(bytes.AsSpan(16, 16));
        packetIndex = bytes[32];
        return true;
    }

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReplicatorKeyHashToBytes(string keyHash)
        => SNetExt_HashUtil.HashHexToBytes(keyHash);

    [HideFromIl2Cpp]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplicatorKeyToHash(string key)
        => SNetExt_HashUtil.KeyToHashHex(key);

    [HideFromIl2Cpp]
    internal static void AllocateReplicator(ISNetExt_MutableReplicator replicator)
    {
        var status = s_registry.Register(replicator);
        switch (status)
        {
            case SNetExt_ReplicatorRegistry.RegisterStatus.InvalidKeyHash:
                _logger.Error($"AllocateReplicator, Invalid KeyHash. Hash: '{replicator.KeyHash}'");
                throw new InvalidOperationException($"Replicator has invalid KeyHash '{replicator.KeyHash}'");
            case SNetExt_ReplicatorRegistry.RegisterStatus.Conflict:
                s_registry.TryGet(SNetExt_KeyHash16.FromHex(replicator.KeyHash), out var existing);
                _logger.Error($"AllocateReplicator, KeyHash conflict. Existing Key: '{existing?.Key}', New Key: '{replicator.Key}', KeyHash: '{replicator.KeyHash}'");
                throw new InvalidOperationException($"Replicator with KeyHash '{replicator.KeyHash}' already exists");
        }
    }

    [HideFromIl2Cpp]
    internal static void AssignReplicatorKey(ISNetExt_MutableReplicator replicator, string keyHash, bool isRecall = false)
    {
        var newHash = SNetExt_KeyHash16.FromHex(keyHash);
        if (newHash.IsEmpty)
        {
            _logger.Error($"AssignReplicatorKey, Invalid KeyHash. Hash: '{keyHash}'");
            return;
        }
        var status = s_registry.Reassign(replicator, newHash);
        if (status == SNetExt_ReplicatorRegistry.RegisterStatus.Conflict)
        {
            _logger.Error($"AssignReplicatorKey, KeyHash conflict on '{keyHash}'");
            throw new InvalidOperationException($"Cannot reassign to KeyHash '{keyHash}': occupied");
        }
    }

    [HideFromIl2Cpp]
    internal static void DeallocateReplicator(ISNetExt_Replicator replicator)
    {
        s_registry.Unregister(replicator);
        if (replicator is ISNetExt_MutableReplicator mut)
            mut.AssignKey(string.Empty);
    }

    [HideFromIl2Cpp]
    public static ISNetExt_Replicator AddManagerReplicator(string key)
    {
        var manager = new SNetExt_Replicator_Manager();
        ((ISNetExt_MutableReplicator)manager).AssignKey(key);
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
        var replicator = new SNetExt_Replicator_SelfManaged();
        ((ISNetExt_MutableReplicator)replicator).AssignKey(key);
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
        if (vanilla == null || vanilla.Key == 0) return false;
        if (s_registry.TryGetByVanilla(vanilla.Pointer, out var existing) && existing != null && existing.IsAlive)
            return true;
        return BindVanillaInternal(vanilla) != null;
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
        var status = s_registry.RegisterVanilla(wrapper);
        if (status != SNetExt_ReplicatorRegistry.RegisterStatus.Success)
        {
            _logger.Error($"BindVanillaInternal: failed to register wrapper, status={status}");
            return null;
        }
        return wrapper;
    }

    [HideFromIl2Cpp]
    internal static bool TryGetVanillaWrapper(SNetwork.IReplicator vanilla, out SNetExt_Replicator_VanillaWrapper wrapper)
        => s_registry.TryGetByVanilla(vanilla.Pointer, out wrapper);

    [HideFromIl2Cpp]
    internal static void DeallocateVanillaWrapper(SNetExt_Replicator_VanillaWrapper wrapper)
    {
        if (!s_registry.UnregisterVanilla(wrapper))
            _logger.Error($"DeallocateVanillaWrapper: Wrapper doesn't exist for Vanilla.Pointer={wrapper.Vanilla.Pointer}");
    }

    private readonly List<SNetExt_ReplicationManager> m_replicationManagers = new();

    [HideFromIl2Cpp]
    public ulong LastSenderID { get; private set; }
}
