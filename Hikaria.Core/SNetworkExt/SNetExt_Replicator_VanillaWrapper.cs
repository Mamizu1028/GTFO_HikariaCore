namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replicator_VanillaWrapper : SNetExt_Replicator
{
    public SNetExt_Replicator_VanillaWrapper(SNetwork.IReplicator vanilla)
    {
        VanillaType = vanilla.Type;
        Key = $"VanillaWrapper_{vanilla.Key}";
    }

    public readonly SNetwork.SNet_ReplicatorType VanillaType;

    public override SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.VanillaWrapper;

    public override bool LocallyOwned => true;

    public override void Despawn()
    {
        SNetExt_Replication.DeallocateReplicator(this);
    }
}
