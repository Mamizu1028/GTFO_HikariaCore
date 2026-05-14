namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replicator_SelfManaged : SNetExt_Replicator, ISNetExt_OwnedReplicator
{
    public override SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.SelfManaged;

    public override bool LocallyOwned => true;

    SNetwork.SNet_Player ISNetExt_OwnedReplicator.OwningPlayer
    {
        get => OwningPlayer;
        set => SetOwningPlayerInternal(value);
    }

    bool ISNetExt_OwnedReplicator.OwnedByMaster
    {
        get => OwnedByMaster;
        set => SetOwnedByMasterInternal(value);
    }

    public override void Despawn()
    {
        SNetExt_Replication.DeallocateReplicator(this);
        ReplicatorSupplier = null;
    }
}
