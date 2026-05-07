namespace Hikaria.Core.SNetworkExt;

public class SNetExt_Replicator_Manager : SNetExt_Replicator
{
    public override SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.Manager;

    public override bool LocallyOwned => true;

    public override void Despawn()
    {
        SNetExt.Logger.Warning(
            $"Despawn called on Manager replicator '{Key}' (KeyHash={KeyHash}); ignored.");
    }
}
