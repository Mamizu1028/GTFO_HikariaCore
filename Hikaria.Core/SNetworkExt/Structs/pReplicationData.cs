using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct pReplicationData
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string PrefabKeyHash;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string ReplicatorKeyHash;

    public bool isRecall;
}
