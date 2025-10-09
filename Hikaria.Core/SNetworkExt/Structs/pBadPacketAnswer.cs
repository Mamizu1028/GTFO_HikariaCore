using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct pBadPacketAnswer
{
    public pBadPacketQuestion question;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string info;
}
