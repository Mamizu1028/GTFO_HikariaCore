using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct pReplicator
{
    public SNetExt_KeyHash16 KeyHash;

    public readonly bool TryGetReplicator(out ISNetExt_Replicator rep)
        => SNetExt_Replication.TryGetReplicatorByKeyHash16(KeyHash, out rep);

    public void SetReplicator(ISNetExt_Replicator rep)
    {
        if (rep != null)
            KeyHash = SNetExt_KeyHash16.FromHex(rep.KeyHash);
        else
            KeyHash = default;
    }

    public readonly bool IsValid() => !KeyHash.IsEmpty;
}
