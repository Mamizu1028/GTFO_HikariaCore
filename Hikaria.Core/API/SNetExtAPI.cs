using Hikaria.Core.SNetworkExt;

namespace Hikaria.Core;

public static class SNetExtAPI
{
    public static bool TryGetVanillaWrapper(SNetwork.IReplicator vanilla, out SNetExt_Replicator_VanillaWrapper wrapper)
    {
        return SNetExt_Replication.TryGetVanillaWrapper(vanilla, out wrapper);
    }
}
