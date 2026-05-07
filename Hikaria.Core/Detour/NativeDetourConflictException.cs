namespace Hikaria.Core.Detour;

public sealed class NativeDetourConflictException : InvalidOperationException
{
    public nint MethodPointer { get; }

    public DetourDescriptor Descriptor { get; }

    internal NativeDetourConflictException(nint methodPointer, DetourDescriptor descriptor = null)
        : base(BuildMessage(methodPointer, descriptor))
    {
        MethodPointer = methodPointer;
        Descriptor    = descriptor;
    }

    private static string BuildMessage(nint ptr, DetourDescriptor d)
        => d != null
            ? $"方法 '{d}' (指针 0x{ptr:X}) 已被另一个 NativeDetour 占用,不允许重复 detour"
            : $"方法指针 0x{ptr:X} 已被另一个 NativeDetour 占用,不允许重复 detour";
}
