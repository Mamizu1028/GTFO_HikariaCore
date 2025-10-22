namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replicator_VanillaWrapper : SNetExt_Replicator
{
    public SNetExt_Replicator_VanillaWrapper(SNetwork.IReplicator replicator)
    {
        VanillaType = replicator.Type;
        Key = $"VanillaWrapper_{replicator.Key}";
        OwnedByMaster = replicator.Cast<SNetwork.SNet_Replicator>().OwnedByMaster;
        OwningPlayer = replicator.OwningPlayer;
    }

    public readonly SNetwork.SNet_ReplicatorType VanillaType;

    public override SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.VanillaWrapper;

    public override void Despawn()
    {
        SNetExt_Replication.DeallocateReplicator(this);
    }
}
