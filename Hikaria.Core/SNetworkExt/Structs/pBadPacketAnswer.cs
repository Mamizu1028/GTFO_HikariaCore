using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public struct pBadPacketAnswer
{
    public pBadPacketQuestion question;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string info;
}
