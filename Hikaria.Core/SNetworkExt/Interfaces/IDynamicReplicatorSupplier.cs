namespace Hikaria.Core.SNetworkExt;

public interface IDynamicReplicatorSupplier<T> : IReplicatorSupplier where T : struct, IDynamicReplication
{
    void OnDespawn();

    void OnSpawn(T spawnData);

    bool TryCollectCaptureData(ref T spawnData, out SNetwork.eCapturePass captureType);
}
