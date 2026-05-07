namespace Hikaria.Core.Detour;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class NativeDetourAttribute : Attribute
{
    public Type DeclaringType { get; }

    public string MethodName { get; }

    public bool Static { get; init; }

    public NativeDetourMember Member { get; init; } = NativeDetourMember.Method;

    public Type[] TypeArguments { get; init; }

    public NativeDetourAttribute(Type declaringType, string methodName)
    {
        DeclaringType = declaringType;
        MethodName    = methodName;
    }

    public NativeDetourAttribute(Type declaringType)
    {
        DeclaringType = declaringType;
        MethodName    = null;
        Member        = NativeDetourMember.Constructor;
    }
}

public enum NativeDetourMember
{
    Method,
    Constructor,
    Getter,
    Setter,
}
