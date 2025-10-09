using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct pBadPacketQuestion
{
    public readonly string GetLookup()
    {
        return $"{replicatorKeyHash}_{packetKeyHash}_{packetIndex}";
    }

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string replicatorKeyHash;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string packetKeyHash;

    public byte packetIndex;
}
