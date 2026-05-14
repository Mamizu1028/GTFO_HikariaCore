using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_PrefabReplicationManager : MonoBehaviour, ISNetExt_Manager
{
    [HideFromIl2Cpp]
    public void OnValidateMasterData() { }

    [HideFromIl2Cpp]
    public void SetupReplicationFor<TData>(Action<ISNetExt_Replicator, bool> callback = null) where TData : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<TData>, TData>(out var replicationManager);
        replicationManager.SpawnDespawnCallback = callback;
    }

    [HideFromIl2Cpp]
    public void ClearPrefabs<TData>() where TData : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<TData>, TData>(out var replicationManager);
        replicationManager.ClearPrefabs();
    }

    [HideFromIl2Cpp]
    public void AddPrefab<TData>(string prefabKey, GameObject prefab) where TData : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<TData>, TData>(out var replicationManager);
        replicationManager.AddPrefab(prefabKey, prefab, null);
    }

    [HideFromIl2Cpp]
    public bool RemovePrefab<TData>(string prefabKey) where TData : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<TData>, TData>(out var replicationManager);
        return replicationManager.RemovePrefab(prefabKey);
    }

    [HideFromIl2Cpp]
    public bool RemovePrefab<TData>(GameObject prefab) where TData : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<TData>, TData>(out var replicationManager);
        return replicationManager.RemovePrefab(prefab);
    }

    [HideFromIl2Cpp]
    private void GetRepManager<TReplicator, TData>(out SNetExt_ReplicationManager<TData, TReplicator> repManager)
        where TReplicator : SNetExt_DynamicReplicator<TData>, new()
        where TData : struct, ISNetExt_DynamicReplication
    {
        Type typeFromHandle = typeof(TData);
        if (m_repManagers.TryGetValue(typeFromHandle, out var existing))
        {
            repManager = existing as SNetExt_ReplicationManager<TData, TReplicator>;
            if (repManager == null)
                throw new InvalidOperationException(
                    $"Manager for {typeFromHandle.Name} was registered with a different TReplicator (existing: {existing.GetType().Name})");
            return;
        }
        repManager = new SNetExt_ReplicationManager<TData, TReplicator>();
        m_repManagers.Add(typeFromHandle, repManager);
    }

    [HideFromIl2Cpp]
    public void Spawn<TData>(GameObject prefab, TData spawnData) where TData : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<TData>, TData>(out var replicationManager);
        replicationManager.Spawn(prefab, spawnData);
    }

    [HideFromIl2Cpp]
    public List<SNetExt_DynamicReplicator<TData>> GetReplicatorList<TData>() where TData : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<TData>, TData>(out var replicationManager);
        return replicationManager.m_replicators;
    }

    [HideFromIl2Cpp]
    public void Setup()
    {
    }

    [HideFromIl2Cpp]
    public void SetupReplication()
    {
    }

    [HideFromIl2Cpp]
    public void OnResetSession()
    {
    }

    [HideFromIl2Cpp]
    public void OnClearAllSpawnedLocal()
    {
        foreach (var kvp in m_repManagers)
        {
            kvp.Value.ClearAllLocal();
        }
    }

    private readonly Dictionary<Type, SNetExt_ReplicationManager> m_repManagers = new(16);
}
