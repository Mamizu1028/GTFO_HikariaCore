using SNetwork;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct pBufferCommand
{
    public SNetExt_BufferType type;

    public SNetExt_BufferOperationType operation;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string bufferKey;

    public ushort bufferID;
}