using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct pBufferCompletion
{
    public SNetExt_BufferType type;

    public pBufferData data;
}
