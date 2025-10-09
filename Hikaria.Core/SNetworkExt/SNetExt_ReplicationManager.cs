using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_ReplicationManager : IReplicatorSupplier
{
    public IReplicator Replicator { get; set; }

    public GameObject gameObject { get; set; }

    public virtual string Key => GetType().FullName;

    public abstract void OnStateCapture();

    public abstract void ClearAllLocal();

    public abstract void OnValidateMasterData();

    protected SNetExt_ReplicationManager()
    {
    }
}

public abstract class SNetExt_ReplicationManager<T> : SNetExt_ReplicationManager where T : struct, IDynamicReplication
{
    public override string Key => $"SNetExt_ReplicationManager<{typeof(T).FullName}>";

    public virtual void DeSpawn(SNetExt_DynamicReplicator<T> replicator)
    {
    }

    protected SNetExt_ReplicationManager()
    {
    }
}

public class SNetExt_ReplicationManager<T, R> : SNetExt_ReplicationManager<T> where T : struct, IDynamicReplication where R : SNetExt_DynamicReplicator<T>, new()
{
    protected IArchiveLogger _logger;

    public override string Key => $"SNetExt_ReplicationManager<{typeof(T).FullName}, {typeof(R).FullName}>";

    public Action<IReplicator, bool> SpawnDespawnCallback
    {
        set
        {
            m_spawnDespawnCallback = value;
            m_hasSpawnDespawnCallback = value != null;
        }
    }

    public SNetExt_ReplicationManager(SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        _logger = LoaderWrapper.CreateArSubLoggerInstance(GetType().Name);
        m_channelType = channelType;
        m_spawnPacket = SNetExt.SubManagerReplicator.CreatePacket<T>(typeof(T).FullName, InternalSpawnCallback);
        m_spawnRequestPacket = SNetExt.SubManagerReplicator.CreatePacket<T>(typeof(T).FullName, InternalSpawnRequestFromSlaveCallback);
        m_despawnRequestPacket = SNetExt.SubManagerReplicator.CreatePacket<pReplicationData>($"{typeof(T).FullName}_{typeof(pReplicationData).FullName}", InternalDeSpawnRequestFromSlaveCallback);
        m_despawnPacket = SNetExt.SubManagerReplicator.CreatePacket<pReplicationData>($"{typeof(T).FullName}_{typeof(pReplicationData).FullName}", InternalDeSpawnCallback_Slave);
        SNetExt.Replication.RegisterReplicationManager(this);
    }

    public void AddPrefab(string key, GameObject prefab, GameObject prefabSyncVersion = null)
    {
        var keyHash = PrefabKeyToHash(key);
        if (m_prefabs.ContainsKey(keyHash))
        {
            _logger.Error($"AddPrefab, Hash Conflict. Key: '{key}', KeyHash: '{keyHash}'");
            return;
        }
        m_prefabLookup.Add(prefab.Pointer, keyHash);
        m_prefabs.Add(keyHash, new(prefab, prefabSyncVersion));
        m_prefabKeyToKeyHash.Add(key, keyHash);
        m_prefabKeyHashToKey.Add(keyHash, key);
    }

    public void ClearPrefabs()
    {
        m_prefabLookup.Clear();
        m_prefabs.Clear();
        m_prefabKeyToKeyHash.Clear();
        m_prefabKeyHashToKey.Clear();
    }

    public bool TryGetPrefabKey(GameObject prefab, out string key, out string keyHash)
    {
        if (!m_prefabLookup.TryGetValue(prefab.Pointer, out keyHash) || !m_prefabKeyHashToKey.TryGetValue(keyHash, out key))
        {
            key = string.Empty;
            keyHash = string.Empty;
            return false;
        }
        return true;
    }

    public bool HasPrefab(GameObject prefab)
    {
        return m_prefabLookup.ContainsKey(prefab.Pointer);
    }

    public void Spawn(GameObject prefab, T spawnData)
    {
        if (!TryGetPrefabKey(prefab, out _, out var keyHash))
        {
            _logger.Error($"Spawn, prefabKey for '{prefab.name}' add prefab before attempting spawn");
            return;
        }
        Spawn(keyHash, spawnData);
    }

    public void Spawn(string prefabKeyHash, T spawnData)
    {
        var replicationData = spawnData.ReplicationData;
        replicationData.PrefabKeyHash = prefabKeyHash;
        spawnData.ReplicationData = replicationData;
        Spawn(spawnData);
    }

    public void InjectOfflinePrespawn(T spawnData, R preSpawnReplicator)
    {
        m_preSpawnedReplicator = preSpawnReplicator;
        InternalSpawnCallback(spawnData);
        m_preSpawnedReplicator = default;
    }

    public void Spawn(T spawnData)
    {
        if (SNetwork.SNet.IsMaster)
        {
            InternalSpawnCallback(spawnData);
            return;
        }
        if (SNetwork.SNet.HasMaster)
        {
            m_spawnRequestPacket.Send(spawnData, m_channelType, SNetwork.SNet.Master);
        }
    }

    private void InternalSpawnRequestFromSlaveCallback(T spawnData)
    {
        if (SNetwork.SNet.IsMaster && OnSpawnRequest(spawnData))
        {
            InternalSpawnCallback(spawnData);
        }
    }

    protected virtual bool OnSpawnRequest(T spawnData)
    {
        return true;
    }

    protected virtual void InternalSpawnCallback(T spawnData)
    {
        var replicationData = spawnData.ReplicationData;
        R r = default;
        GameObject gameObject = null;
        bool flag = m_preSpawnedReplicator != null;
        if (flag)
        {
            r = m_preSpawnedReplicator;
            gameObject = r.ReplicatorSupplier.gameObject;
        }
        if (flag || TryGetGameObjectReplicator(spawnData, out r, out gameObject))
        {
            if (SNetwork.SNet.IsMaster && !spawnData.ReplicationData.isRecall)
            {
                var dynamicReplicatorSupplier = LinkSupplier(r, gameObject);
                SNetExt_Replication.AllocateReplicator(r);
                replicationData.ReplicatorKeyHash = r.KeyHash;
                spawnData.ReplicationData = replicationData;
                r.SetSpawnData(spawnData);
                RegisterDynamicReplicator(r);
                dynamicReplicatorSupplier.OnSpawn(spawnData);
                m_spawnPacket.Send(spawnData, SNetwork.SNet_ChannelType.GameOrderCritical);
            }
            else
            {
                var dynamicReplicatorSupplier = LinkSupplier(r, gameObject);
                SNetExt_Replication.AssignReplicatorKey(r, replicationData.ReplicatorKeyHash, spawnData.ReplicationData.isRecall);
                r.SetSpawnData(spawnData);
                RegisterDynamicReplicator(r);
                dynamicReplicatorSupplier.OnSpawn(spawnData);
            }
            OnSpawn(spawnData, r);
            m_internalSpawnCallbackReturnReplicator = r;
            return;
        }
        _logger.Error("InternalSpawnCallback, Prefab not found " + replicationData.PrefabKeyHash);
        m_internalSpawnCallbackReturnReplicator = default;
    }

    private IDynamicReplicatorSupplier<T> LinkSupplier(R replicator, GameObject go)
    {
        var supplier = go.GetComponent<IReplicatorSupplier>() as IDynamicReplicatorSupplier<T>;
        replicator.ReplicatorSupplier = supplier;
        supplier.Replicator = replicator;
        return supplier;
    }

    public override void DeSpawn(SNetExt_DynamicReplicator<T> incomingReplicator)
    {
        if (incomingReplicator is null)
            return;

        R r = (R)(object)incomingReplicator;
        if (SNetwork.SNet.IsMaster)
        {
            T spawnData = r.GetSpawnData();
            m_despawnPacket.Send(spawnData.ReplicationData, m_channelType);
        }
        OnDeSpawn(r);
        UnregisterDynamicReplicator(r);
    }

    public override void ClearAllLocal()
    {
        for (int i = m_replicators.Count - 1; i > -1; i--)
        {
            R r = m_replicators[i];
            if (r != null)
            {
                OnDeSpawn(r);
                UnregisterDynamicReplicator(r);
            }
        }
        m_replicators.Clear();
    }

    private void InternalDeSpawnRequestFromSlaveCallback(pReplicationData despawnData)
    {
        if (SNetwork.SNet.IsMaster && SNetExt_Replication.TryGetReplicator(despawnData, out var replicator))
        {
            if (replicator is not SNetExt_DynamicReplicator<T>)
                return;

            DeSpawn(replicator as SNetExt_DynamicReplicator<T>);
        }
    }

    private void InternalDeSpawnCallback_Slave(pReplicationData despawnData)
    {
        if (SNetExt_Replication.TryGetReplicator(despawnData, out var replicator))
        {
            OnDeSpawn(replicator as R);
            UnregisterDynamicReplicator(replicator as R);
        }
    }

    public virtual bool UseSyncPrefab(T spawnData)
    {
        return false;
    }

    protected R AddReplicator(T spawnData, GameObject go)
    {
        return new R();
    }

    public virtual void SetOwnership(T spawnData, R replicator)
    {
        replicator.OwnedByMaster = true;
    }

    protected bool TryGetGameObjectReplicator(T spawnData, out R replicator, out GameObject go)
    {
        if (!TryGetInstanceReplicator(spawnData, out replicator, out go))
            return false;
        SetOwnership(spawnData, replicator);
        return true;
    }

    protected virtual bool TryGetInstanceReplicator(T spawnData, out R replicator, out GameObject go)
    {
        var replicationData = spawnData.ReplicationData;
        if (!m_prefabs.ContainsKey(replicationData.PrefabKeyHash))
        {
            replicator = null;
            go = null;
            return false;
        }
        GameObject gameObject;
        if (!UseSyncPrefab(spawnData))
        {
            gameObject = m_prefabs[replicationData.PrefabKeyHash].normalVersion;
        }
        else
        {
            gameObject = m_prefabs[replicationData.PrefabKeyHash].syncVersion;
        }
        go = UnityEngine.Object.Instantiate(gameObject, spawnData.Position, spawnData.Rotation);
        replicator = AddReplicator(spawnData, go);
        return true;
    }

    private void RegisterDynamicReplicator(R replicator)
    {
        replicator.SetManager(this);
        m_replicators.Add(replicator);
    }

    private void UnregisterDynamicReplicator(R replicator)
    {
        replicator.SetManager(null);
        m_replicators.Remove(replicator);
        SNetExt_Replication.DeallocateReplicator(replicator);
    }

    protected virtual void OnSpawn(T spawnData, R replicator)
    {
        if (m_hasSpawnDespawnCallback)
        {
            m_spawnDespawnCallback(replicator, true);
        }
    }

    protected virtual void OnDeSpawn(R replicator)
    {
        if (m_hasSpawnDespawnCallback)
        {
            m_spawnDespawnCallback(replicator, false);
        }
        if (replicator.ReplicatorSupplier != null)
        {
            var supplier = replicator.ReplicatorSupplier as IDynamicReplicatorSupplier<T>;
            supplier?.OnDespawn();
            replicator.ReplicatorSupplier = null;
        }
    }

    public override void OnStateCapture()
    {
        for (int i = 0; i < m_replicators.Count; i++)
        {
            if (m_replicators[i].TryInternalCollectCapture(out var t, out var type))
            {
                m_spawnPacket.CaptureToBuffer(t, type);
            }
        }
    }

    public override void OnValidateMasterData()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] PrefabKeyToBytes(string key)
    {
        using MD5 md5 = MD5.Create();
        return md5.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string PrefabKeyToHash(string key)
    {
        using MD5 md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
    }

    public List<R> m_replicators = new();

    private SNetwork.SNet_ChannelType m_channelType;

    protected R m_preSpawnedReplicator;
    protected R m_internalSpawnCallbackReturnReplicator;
    protected SNetExt_ReplicatedPacket<T> m_spawnPacket;
    protected SNetExt_ReplicatedPacket<T> m_spawnRequestPacket;
    protected SNetExt_ReplicatedPacket<pReplicationData> m_despawnRequestPacket;
    protected SNetExt_ReplicatedPacket<pReplicationData> m_despawnPacket;
    protected Dictionary<string, (GameObject normalVersion, GameObject syncVersion)> m_prefabs = new();
    protected Dictionary<string, string> m_prefabKeyToKeyHash = new();
    protected Dictionary<string, string> m_prefabKeyHashToKey = new();
    protected Dictionary<IntPtr, string> m_prefabLookup = new();
    protected bool m_hasSpawnDespawnCallback;
    protected Action<IReplicator, bool> m_spawnDespawnCallback;
    protected Vector3 m_tempPosition;
    protected Quaternion m_tempQuaternion;
}