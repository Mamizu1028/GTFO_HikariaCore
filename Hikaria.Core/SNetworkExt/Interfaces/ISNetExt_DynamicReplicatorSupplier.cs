namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_DynamicReplicatorSupplier<T> : ISNetExt_ReplicatorSupplier where T : struct, ISNetExt_DynamicReplication
{
    void OnDespawn();

    void OnSpawn(T spawnData);

    bool TryCollectCaptureData(ref T spawnData, out SNetExt_CapturePass captureType);
}
