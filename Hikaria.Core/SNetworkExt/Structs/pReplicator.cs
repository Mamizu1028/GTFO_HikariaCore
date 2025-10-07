using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public struct pReplicator
{
    public readonly bool TryGetReplicator(out IReplicator rep)
    {
        return SNetExt_Replication.TryGetReplicator(KeyHash, out rep);
    }

    public void SetReplicator(IReplicator rep)
    {
        if (rep != null)
        {
            KeyHash = rep.KeyHash;
        }
    }

    public readonly bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(KeyHash);
    }

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string KeyHash;
}
