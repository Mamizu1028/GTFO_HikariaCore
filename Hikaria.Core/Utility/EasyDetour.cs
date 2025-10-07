using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.Utility;

public interface IEasyDetour
{
    INativeDetour NativeDetour { get; }
    bool Apply();
    void Unpatch();
}

public abstract class EasyDetourBase<TDelegate> : IEasyDetour, IDisposable where TDelegate : Delegate
{
    private TDelegate _original;
    private INativeDetour _detour;

    ~EasyDetourBase()
    {
        Dispose(false);
    }

    public abstract DetourDescriptor Descriptor { get; }

    public abstract TDelegate DetourTo { get; }

    public TDelegate Original => _original;
    public INativeDetour NativeDetour => _detour;

    public bool Apply()
    {
        if (_detour != null)
        {
            _detour.Apply();
            return true;
        }

        return EasyDetour.CreateAndApply(Descriptor, DetourTo, out _original, out _detour);
    }

    public void Unpatch()
    {
        if (_detour != null)
        {
            _detour.Undo();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_detour != null)
        {
            EasyDetour.s_NativeDetours.Remove(_detour);
            _detour.Undo();
            _detour.Free();
            _detour.Dispose();
            _detour = null;
        }
    }
}

public unsafe static class EasyDetour
{
    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateArSubLoggerInstance(nameof(EasyDetour));

    public delegate void StaticVoidDelegate(Il2CppMethodInfo* methodInfo);
    public delegate void InstanceVoidDelegate(IntPtr instancePtr, Il2CppMethodInfo* methodInfo);

    internal static HashSet<INativeDetour> s_NativeDetours = new HashSet<INativeDetour>();

    public static bool CreateAndApply<T>(DetourDescriptor descriptor, T to, out T original, out INativeDetour detour) where T : Delegate
    {
        try
        {
            var ptr = descriptor.GetMethodPointer();
            detour = INativeDetour.CreateAndApply(ptr, to, out original);
            if (detour != null)
            {
                s_NativeDetours.Add(detour);
                Logger.Success($"NativeDetour 创建成功: {descriptor}");
                return true;
            }
            Logger.Fail($"NativeDetour 创建失败: {descriptor}");
        }
        catch (Exception ex)
        {
            Logger.Error($"NativeDetour 创建错误");
            Logger.Exception(ex);
        }
        original = null;
        detour = null;
        return false;
    }

    public static bool CreateAndApply<T>(out IEasyDetour easyDetour) where T : IEasyDetour
    {
        easyDetour = (IEasyDetour)Activator.CreateInstance(typeof(T));
        return easyDetour.Apply();
    }
}

public struct DetourDescriptor
{
    public Type Type;
    public string MethodName;
    public Type[] ArgTypes;
    public Type ReturnType;
    public bool IsGeneric;

    public unsafe nint GetMethodPointer()
    {
        ValidateDescriptor();

        var typePtr = Il2CppClassPointerStore.GetNativeClassPointer(Type);
        var returnTypeName = GetFullName(ReturnType);
        var argTypeNames = ArgTypes?.Select(GetFullName).ToArray() ?? Array.Empty<string>();

        var methodPtr = (void**)IL2CPP.GetIl2CppMethod(typePtr, IsGeneric, MethodName, returnTypeName, argTypeNames).ToPointer();
        return methodPtr != null ? (nint)(*methodPtr) : 0;
    }

    private void ValidateDescriptor()
    {
        if (Type == null) throw new ArgumentNullException(nameof(Type), "目标类型不可为空");
        if (ReturnType == null) throw new ArgumentNullException(nameof(ReturnType), "返回类型不可为空");
        if (string.IsNullOrWhiteSpace(MethodName)) throw new ArgumentException("目标方法名称不可为空", nameof(MethodName));
    }

    private static string GetFullName(Type type)
    {
        bool isPointer = type.IsPointer;
        if (isPointer) type = type.GetElementType();

        return isPointer ? type.MakePointerType().FullName : type.FullName;
    }

    public override string ToString()
    {
        return $"{Type.FullName}.{MethodName}{(IsGeneric ? $"<{string.Join(", ", Type.GenericTypeArguments.Select(arg => arg.FullName))}>" : string.Empty)}({string.Join(", ", ArgTypes.Select(arg => arg.FullName))})";
    }
}
