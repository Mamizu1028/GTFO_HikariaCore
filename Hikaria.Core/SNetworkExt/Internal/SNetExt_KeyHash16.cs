using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct SNetExt_KeyHash16 : IEquatable<SNetExt_KeyHash16>
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
        try
        {
            byte[] tmp = Convert.FromHexString(hex32);
            return FromSpan(tmp);
        }
        catch (FormatException)
        {
            return default;
        }
    }

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
