namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replicator_SelfManaged : SNetExt_Replicator
{
    public override SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.SelfManaged;

    public override bool LocallyOwned => true;

    public override void Despawn()
    {
        SNetExt_Replication.DeallocateReplicator(this);
        ReplicatorSupplier = null;
    }
}
