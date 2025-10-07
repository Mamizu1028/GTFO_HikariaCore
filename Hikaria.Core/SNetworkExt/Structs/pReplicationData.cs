using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public struct pReplicationData
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string PrefabKeyHash;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string ReplicatorKeyHash;

    public bool isRecall;
}
