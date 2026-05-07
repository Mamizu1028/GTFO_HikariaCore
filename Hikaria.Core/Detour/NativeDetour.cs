using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.Detour;

public sealed class DetourDescriptor
{
    public Type Type { get; init; }

    public string MethodName { get; init; }

    public Type[] ArgTypes { get; init; }

    public Type ReturnType { get; init; }

    public bool IsGeneric { get; init; }

    public Type[] GenericTypeArguments { get; init; }

    private nint _cachedPtr;
    private bool _resolved;
    private readonly object _resolveLock = new();

    public unsafe nint GetMethodPointer()
    {
        if (Volatile.Read(ref _resolved)) return _cachedPtr;

        lock (_resolveLock)
        {
            if (_resolved) return _cachedPtr;

            ValidateDescriptor();

            var typePtr = Il2CppClassPointerStore.GetNativeClassPointer(Type);
            var returnTypeName = GetFullName(ReturnType);
            var argTypeNames = ArgTypes is { Length: > 0 }
                ? ArgTypes.Select(GetFullName).ToArray()
                : Array.Empty<string>();

            var methodPtr = (void**)IL2CPP.GetIl2CppMethod(
                typePtr, IsGeneric, MethodName, returnTypeName, argTypeNames).ToPointer();
            _cachedPtr = methodPtr != null ? (nint)(*methodPtr) : 0;
            Volatile.Write(ref _resolved, true);
            return _cachedPtr;
        }
    }

    private void ValidateDescriptor()
    {
        if (Type == null) throw new ArgumentNullException(nameof(Type), "目标类型不可为空");
        if (ReturnType == null) throw new ArgumentNullException(nameof(ReturnType), "返回类型不可为空");
        if (string.IsNullOrWhiteSpace(MethodName)) throw new ArgumentException("目标方法名称不可为空", nameof(MethodName));
    }

    private static string GetFullName(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        int pointerDepth = 0;
        while (type.IsPointer)
        {
            var elem = type.GetElementType()
                ?? throw new InvalidOperationException("无效的指针类型: GetElementType 返回 null");
            type = elem;
            pointerDepth++;
        }

        var name = type.FullName ?? type.Name;
        if (pointerDepth == 0) return name;
        return name + new string('*', pointerDepth);
    }

    public override string ToString()
    {
        var argTypes = ArgTypes ?? Array.Empty<Type>();
        var generic = IsGeneric && GenericTypeArguments is { Length: > 0 } gargs
            ? $"<{string.Join(", ", gargs.Select(a => a?.FullName))}>"
            : string.Empty;
        return $"{Type?.FullName}.{MethodName}{generic}({string.Join(", ", argTypes.Select(a => a?.FullName))})";
    }
}

public interface INativeDetourHandle : IDisposable
{
    DetourDescriptor Descriptor { get; }
    INativeDetour NativeDetour { get; }
    bool IsApplied { get; }
    bool Apply();
    void Unpatch();
}

public sealed class NativeDetourHandle<TDelegate> : INativeDetourHandle
    where TDelegate : Delegate
{
    private readonly object _sync = new();
    private TDelegate _original;
    private INativeDetour _detour;
    private bool _applied;
    private int _disposed;   // 0 = alive, 1 = disposed

    public DetourDescriptor Descriptor { get; }

    public TDelegate Hook { get; }

    public TDelegate Original
    {
        get
        {
            ThrowIfDisposed();
            var v = Volatile.Read(ref _original) ?? throw new InvalidOperationException(
                    $"NativeDetourHandle 尚未 Apply,Original 不可用: {Descriptor}");
            return v;
        }
    }

    public INativeDetour NativeDetour => _detour;
    public bool IsApplied => Volatile.Read(ref _applied);

    internal NativeDetourHandle(DetourDescriptor descriptor, TDelegate hook)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Hook       = hook       ?? throw new ArgumentNullException(nameof(hook));
    }

    public bool Apply()
    {
        ThrowIfDisposed();
        lock (_sync)
        {
            if (_applied) return true;

            if (_detour != null)
            {
                try
                {
                    _detour.Apply();
                    Volatile.Write(ref _applied, true);
                    return true;
                }
                catch (Exception ex)
                {
                    global::Hikaria.Core.Detour.NativeDetour.LogException("NativeDetour 重新启用错误", Descriptor, ex);
                    return false;
                }
            }

            if (global::Hikaria.Core.Detour.NativeDetour.CreateAndApplyCore(Descriptor, Hook, out _original, out _detour))
            {
                Volatile.Write(ref _applied, true);
                return true;
            }
            return false;
        }
    }

    public void Unpatch()
    {
        ThrowIfDisposed();
        lock (_sync)
        {
            if (!_applied || _detour == null) return;
            try { _detour.Undo(); }
            catch (Exception ex) { global::Hikaria.Core.Detour.NativeDetour.LogException("NativeDetour 撤销错误", Descriptor, ex); }
            Volatile.Write(ref _applied, false);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        DisposeCore();
    }

    private void DisposeCore()
    {
        lock (_sync)
        {
            var d = _detour;
            _detour = null;
            _original = null;
            Volatile.Write(ref _applied, false);

            if (d == null) return;

            global::Hikaria.Core.Detour.NativeDetour.Unregister(d);
            try { d.Undo(); } catch { /* swallow */ }
            try { d.Free(); } catch { /* swallow */ }
            try { d.Dispose(); } catch { /* swallow */ }
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(GetType().FullName);
    }
}

public unsafe static class NativeDetour
{
    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger => LazyInitializer.EnsureInitialized(
        ref _logger,
        () => LoaderWrapper.CreateArSubLoggerInstance(nameof(NativeDetour)));

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void StaticVoidDelegate(Il2CppMethodInfo* methodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void InstanceVoidDelegate(IntPtr self, Il2CppMethodInfo* methodInfo);

    public static NativeDetourHandle<T> Create<T>(DetourDescriptor descriptor, T hook)
        where T : Delegate
        => new(descriptor, hook);

    public static NativeDetourHandle<T> CreateAndApply<T>(DetourDescriptor descriptor, T hook)
        where T : Delegate
    {
        var h = new NativeDetourHandle<T>(descriptor, hook);
        h.Apply();
        return h;
    }

    internal static void DisposeAll() => NativeDetourRegistry.DisposeAll();


    internal static void Unregister(INativeDetour d) => NativeDetourRegistry.Unregister(d);

    internal static void LogFail(string what, DetourDescriptor descriptor)
        => Logger.Fail($"NativeDetour 应用失败 ({what}): {descriptor}");

    internal static void LogException(string what, DetourDescriptor descriptor, Exception ex)
    {
        Logger.Error($"{what}: {descriptor}");
        Logger.Exception(ex);
    }

    internal static bool CreateAndApplyCore<T>(
        DetourDescriptor descriptor, T hook, out T original, out INativeDetour detour)
        where T : Delegate
    {
        original = null;
        detour = null;
        if (descriptor == null)
        {
            Logger.Fail("NativeDetour 应用失败 (descriptor 为 null)");
            return false;
        }
        if (hook == null)
        {
            LogFail("hook 委托为 null", descriptor);
            return false;
        }

        nint ptr;
        try
        {
            ptr = descriptor.GetMethodPointer();
            if (ptr == 0)
            {
                LogFail("找不到 IL2CPP 方法", descriptor);
                return false;
            }

            // 快速路径:启动前已被占用直接抛,不浪费 native 资源
            if (NativeDetourRegistry.IsRegistered(ptr))
                throw new NativeDetourConflictException(ptr, descriptor);

            detour = INativeDetour.CreateAndApply(ptr, hook, out original);
            if (detour == null)
            {
                LogFail("INativeDetour.CreateAndApply 返回 null", descriptor);
                return false;
            }

            try
            {
                NativeDetourRegistry.Register(ptr, detour);
            }
            catch (NativeDetourConflictException)
            {
                SafeReleaseDetour(detour);
                detour   = null;
                original = null;
                throw new NativeDetourConflictException(ptr, descriptor);
            }

            Logger.Success($"NativeDetour 已应用: {descriptor}");
            return true;
        }
        catch (NativeDetourConflictException)
        {
            // 显式让冲突异常向外传,不被通用 catch 吞掉
            throw;
        }
        catch (Exception ex)
        {
            LogException("NativeDetour 应用错误", descriptor, ex);
            if (detour != null) { SafeReleaseDetour(detour); detour = null; }
            original = null;
            return false;
        }
    }

    private static void SafeReleaseDetour(INativeDetour d)
    {
        try { d.Undo();    } catch { }
        try { d.Free();    } catch { }
        try { d.Dispose(); } catch { }
    }
}
