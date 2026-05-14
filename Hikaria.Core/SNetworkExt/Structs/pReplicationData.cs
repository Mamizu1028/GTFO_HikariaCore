using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct pReplicationData
{
    public SNetExt_KeyHash16 PrefabKeyHash;

    public SNetExt_KeyHash16 ReplicatorKeyHash;

    [MarshalAs(UnmanagedType.U1)]
    public bool isRecall;
}
