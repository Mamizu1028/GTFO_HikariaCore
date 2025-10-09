using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
public struct pBufferData
{
    public ulong sendingPlayerLookup;

    public ulong levelChecksum;

    public ushort bufferID;

    public float progressionTime;

    public ulong recallCount;
}
