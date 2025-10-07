namespace Hikaria.Core.SNetworkExt;

public struct pStateReplicatorProvider
{
    public readonly bool TryGet<S>(out ISNetExt_StateReplicatorProvider<S> provider) where S : struct
    {
        if (pRep.TryGetReplicator(out var replicator) && replicator != null && replicator.ReplicatorSupplier != null && replicator.ReplicatorSupplier is SNetExt_StateReplicator<S> stateReplicator)
        {
            provider = stateReplicator.m_provider;
            return true;
        }
        provider = null;
        return false;
    }

    public readonly bool TryGet<S, I>(out ISNetExt_StateReplicatorProvider<S, I> provider) where S : struct where I : struct
    {
        if (pRep.TryGetReplicator(out var replicator) && replicator != null && replicator.ReplicatorSupplier != null && replicator.ReplicatorSupplier is SNetExt_StateReplicator<S, I> stateReplicator)
        {
            provider = stateReplicator.m_provider;
            return true;
        }
        provider = null;
        return false;
    }

    internal void Set<S>(SNetExt_StateReplicator<S> stateReplicator) where S : struct
    {
        if (stateReplicator != null)
        {
            pRep.SetReplicator(stateReplicator.Replicator);
            return;
        }
        pRep.SetReplicator(null);
    }

    internal void Set<S, I>(SNetExt_StateReplicator<S, I> stateReplicator) where S : struct where I : struct
    {
        if (stateReplicator != null)
        {
            pRep.SetReplicator(stateReplicator.Replicator);
            return;
        }
        pRep.SetReplicator(null);
    }

    private pReplicator pRep;
}
