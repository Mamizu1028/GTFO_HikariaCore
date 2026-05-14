using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct SNetExt_KeyHash16 : IEquatable<SNetExt_KeyHash16>
{
    public readonly ulong Lower;
    public readonly ulong Upper;

    public SNetExt_KeyHash16(ulong lower, ulong upper)
    {
        Lower = lower;
        Upper = upper;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SNetExt_KeyHash16 FromSpan(ReadOnlySpan<byte> bytes16)
    {
        if (bytes16.Length < 16)
            throw new ArgumentException("need 16 bytes", nameof(bytes16));
        return new SNetExt_KeyHash16(
            BinaryPrimitives.ReadUInt64LittleEndian(bytes16),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes16.Slice(8)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SNetExt_KeyHash16 FromHex(string hex32)
    {
        if (string.IsNullOrWhiteSpace(hex32) || hex32.Length != 32)
            return default;
        Span<byte> bytes = stackalloc byte[16];
        if (!TryParseHex(hex32.AsSpan(), bytes)) return default;
        return FromSpan(bytes);
    }

    private static bool TryParseHex(ReadOnlySpan<char> hex, Span<byte> output)
    {
        for (int i = 0; i < 16; i++)
        {
            int hi = ParseNibble(hex[i * 2]);
            int lo = ParseNibble(hex[i * 2 + 1]);
            if (hi < 0 || lo < 0) return false;
            output[i] = (byte)((hi << 4) | lo);
        }
        return true;
    }

    private static int ParseNibble(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteTo(Span<byte> destination16)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination16, Lower);
        BinaryPrimitives.WriteUInt64LittleEndian(destination16.Slice(8), Upper);
    }

    public string ToHex()
    {
        Span<byte> bytes = stackalloc byte[16];
        WriteTo(bytes);
        return Convert.ToHexString(bytes);
    }

    public bool IsEmpty => Lower == 0 && Upper == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(SNetExt_KeyHash16 other) => Lower == other.Lower && Upper == other.Upper;

    public override bool Equals(object obj) => obj is SNetExt_KeyHash16 other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        ulong x = Lower ^ (Upper * 0x9E3779B97F4A7C15UL);
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return (int)(x ^ (x >> 32));
    }

    public static bool operator ==(SNetExt_KeyHash16 a, SNetExt_KeyHash16 b) => a.Equals(b);
    public static bool operator !=(SNetExt_KeyHash16 a, SNetExt_KeyHash16 b) => !a.Equals(b);

    public override string ToString() => ToHex();
}
