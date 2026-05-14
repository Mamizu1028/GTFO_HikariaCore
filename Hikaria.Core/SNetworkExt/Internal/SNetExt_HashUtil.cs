using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Hikaria.Core.SNetworkExt;

internal static class SNetExt_HashUtil
{
    [ThreadStatic]
    private static MD5 t_md5;

    private static readonly Dictionary<string, string> s_keyHexCache = new(65535);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MD5 GetMD5() => t_md5 ??= MD5.Create();

    public static string KeyToHashHex(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        lock (s_keyHexCache)
        {
            if (s_keyHexCache.TryGetValue(key, out var cached)) return cached;
        }
        var md5 = GetMD5();

        string hex;
        int byteCount = Encoding.UTF8.GetByteCount(key);
        if (byteCount <= 256)
        {
            Span<byte> input = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(key, input);
            Span<byte> output = stackalloc byte[16];
            md5.TryComputeHash(input, output, out _);
            hex = Convert.ToHexString(output);
        }
        else
        {
            hex = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }

        lock (s_keyHexCache)
        {
            s_keyHexCache[key] = hex;
        }
        return hex;
    }

    public static void KeyToHashBytes(string key, Span<byte> destination16)
    {
        if (destination16.Length < 16)
            throw new ArgumentException("destination must be at least 16 bytes", nameof(destination16));
        if (string.IsNullOrWhiteSpace(key))
        {
            destination16.Slice(0, 16).Clear();
            return;
        }
        var md5 = GetMD5();
        int byteCount = Encoding.UTF8.GetByteCount(key);
        if (byteCount <= 256)
        {
            Span<byte> input = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(key, input);
            md5.TryComputeHash(input, destination16, out _);
            return;
        }
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
        bytes.AsSpan(0, 16).CopyTo(destination16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] HashHexToBytes(string keyHash)
    {
        return Convert.FromHexString(keyHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string HashBytesToHex(ReadOnlySpan<byte> hash16)
    {
        return Convert.ToHexString(hash16);
    }
}
