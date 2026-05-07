using System.Buffers;
using System.Runtime.CompilerServices;

namespace Hikaria.Core.SNetworkExt;

internal static class SNetExt_BufferPool
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Rent(int minimumLength) => ArrayPool<byte>.Shared.Rent(minimumLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(byte[] buffer, bool clear = false)
    {
        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer, clear);
    }
}
