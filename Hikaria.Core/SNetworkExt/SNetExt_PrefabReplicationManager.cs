using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_PrefabReplicationManager : MonoBehaviour, ISNetExt_Manager
{
    public void OnValidateMasterData()
    {
    }

    public void SetupReplicationFor<B>(Action<ISNetExt_Replicator, bool> callback = null) where B : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<B>, B>(out var replicationManager);
        replicationManager.SpawnDespawnCallback = callback;
    }

    public void ClearPrefabs<B>() where B : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<B>, B>(out var replicationManager);
        replicationManager.ClearPrefabs();
    }

    public void AddPrefab<B>(string prefabKey, GameObject prefab) where B : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<B>, B>(out var replicationManager);
        replicationManager.AddPrefab(prefabKey, prefab, null);
    }

    private void GetRepManager<A, B>(out SNetExt_ReplicationManager<B, A> repManager) where A : SNetExt_DynamicReplicator<B>, new() where B : struct, ISNetExt_DynamicReplication
    {
        Type typeFromHandle = typeof(B);
        if (m_repManagers.TryGetValue(typeFromHandle, out var replicationManager))
        {
            repManager = replicationManager as SNetExt_ReplicationManager<B, A>;
            return;
        }
        repManager = new SNetExt_ReplicationManager<B, A>();
        replicationManager = repManager;
        m_repManagers.Add(typeFromHandle, replicationManager);
    }

    public void Spawn<B>(GameObject prefab, B spawnData) where B : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<B>, B>(out var replicationManager);
        replicationManager.Spawn(prefab, spawnData);
    }

    public List<SNetExt_DynamicReplicator<B>> GetReplicatorList<B>() where B : struct, ISNetExt_DynamicReplication
    {
        GetRepManager<SNetExt_DynamicReplicator<B>, B>(out var replicationManager);
        return replicationManager.m_replicators;
    }

    public void Setup()
    {
    }

    public void SetupReplication()
    {
    }

    public void OnResetSession()
    {
    }

    public void OnClearAllSpawnedLocal()
    {
        foreach (var kvp in m_repManagers)
        {
            kvp.Value.ClearAllLocal();
        }
    }

    private readonly Dictionary<Type, SNetExt_ReplicationManager> m_repManagers = new();
}
