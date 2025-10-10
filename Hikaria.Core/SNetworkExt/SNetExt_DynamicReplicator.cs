namespace Hikaria.Core.SNetworkExt;

public class SNetExt_DynamicReplicator<T> : SNetExt_Replicator where T : struct, ISNetExt_DynamicReplication
{
    public override SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.Dynamic;

    public void SetManager(SNetExt_ReplicationManager<T> manager)
    {
        m_manager = manager;
        m_isRegistered = manager != null;
    }

    public bool HasManager => m_manager != null;

    internal void SetSpawnData(T spawnData)
    {
        m_spawnData = spawnData;
    }

    public T GetSpawnData()
    {
        return m_spawnData;
    }

    public bool TryInternalCollectCapture(out T spawnData, out SNetExt_CapturePass captureType)
    {
        if (ReplicatorSupplier is ISNetExt_DynamicReplicatorSupplier<T> supplier && supplier.TryCollectCaptureData(ref m_spawnData, out captureType))
        {
            var replicationData = m_spawnData.ReplicationData;
            replicationData.isRecall = true;
            m_spawnData.ReplicationData = replicationData;
            spawnData = m_spawnData;
            return true;
        }
        captureType = SNetExt_CapturePass.Skip;
        spawnData = new T();
        return false;
    }

    public override void Despawn()
    {
        if (m_isRegistered && SNetwork.SNet.IsMaster)
        {
            m_manager.DeSpawn(this);
            return;
        }
        if (ReplicatorSupplier != null && ReplicatorSupplier.gameObject != null)
        {
            UnityEngine.Object.Destroy(ReplicatorSupplier.gameObject);
            ReplicatorSupplier = null;
        }
    }

    private bool m_isRegistered;

    private SNetExt_ReplicationManager<T> m_manager;

    private T m_spawnData;
}
