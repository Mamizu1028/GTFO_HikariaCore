using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct pReplicator
{
    public readonly bool TryGetReplicator(out IReplicator rep)
    {
        return SNetExt_Replication.TryGetReplicatorByKeyHash(KeyHash, out rep);
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
