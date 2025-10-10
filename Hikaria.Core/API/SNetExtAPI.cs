using Hikaria.Core.Features.Dev;
using Hikaria.Core.SNetworkExt;

namespace Hikaria.Core;

public static class SNetExtAPI
{
    public static bool TryGetReplicatorWrapper(SNetwork.IReplicator vanilla, out ISNetExt_Replicator wrapper)
    {
        return SNetExtAPI_Impl.TryGetVanillaReplicatorWrapper(vanilla.Key, out wrapper);
    }
}
