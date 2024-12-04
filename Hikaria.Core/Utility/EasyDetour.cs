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


public abstract class EasyDetourBase<TDelegate> : IEasyDetour where TDelegate : Delegate
{
    ~EasyDetourBase()
    {
        Unpatch();
    }

    public abstract TDelegate DetourTo { get; }
    public abstract DetourDescriptor Descriptor { get; }

    public TDelegate Original => _original;
    public INativeDetour NativeDetour => _detour;

    private TDelegate _original;
    private INativeDetour _detour;

    public bool Apply()
    {
        if (_detour != null)
        {
            _detour.Undo();
            _detour.Free();
            _detour.Dispose();
        }

        return EasyDetour.CreateAndApply(Descriptor, DetourTo, out _original, out _detour);
    }

    public void Unpatch()
    {
        if (_detour != null)
        {
            _detour.Undo();
            _detour.Free();
            _detour.Dispose();
        }
    }
}


public unsafe static class EasyDetour
{
    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateArSubLoggerInstance(nameof(EasyDetour));

    public delegate void StaticVoidDelegate(Il2CppMethodInfo* methodInfo);

    public delegate void InstanceVoidDelegate(IntPtr instancePtr, Il2CppMethodInfo* methodInfo);

    public static bool CreateAndApply<T>(DetourDescriptor descriptor, T to, out T original, out INativeDetour detour) where T : Delegate
    {
        try
        {
            var ptr = descriptor.GetMethodPointer();
            detour = INativeDetour.CreateAndApply(ptr, to, out original);
            var result = detour != null;
            if (result)
            {
                Logger.Success($"NativeDetour Success: {descriptor}");
            }
            else
            {
                Logger.Fail($"NativeDetour Fail: {descriptor}");
            }
            return result;
        }
        catch (Exception e)
        {
            Logger.Error($"Exception Thrown while creating Detour:");
            Logger.Error(e.ToString());
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
    public Type ReturnType;
    public Type[] ArgTypes;
    public string MethodName;
    public bool IsGeneric;

    public unsafe nint GetMethodPointer()
    {
        if (Type == null)
        {
            throw new MissingFieldException($"Field {nameof(Type)} is not set!");
        }

        if (ReturnType == null)
        {
            throw new MissingFieldException($"Field {nameof(ReturnType)} is not set! If you mean 'void' do typeof(void)");
        }

        if (string.IsNullOrEmpty(MethodName))
        {
            throw new MissingFieldException($"Field {nameof(MethodName)} is not set or valid!");
        }

        var type = Il2CppType.From(Type, throwOnFailure: true);
        var typePtr = Il2CppClassPointerStore.GetNativeClassPointer(Type);

        var returnTypeName = GetFullName(ReturnType);
        string[] argTypeNames;
        if (ArgTypes == null || ArgTypes.Length <= 0)
        {
            argTypeNames = Array.Empty<string>();
        }
        else
        {
            var length = ArgTypes.Length;
            argTypeNames = new string[length];
            for (int i = 0; i < length; i++)
            {
                var argType = ArgTypes[i];
                argTypeNames[i] = GetFullName(argType);
            }
        }

        var methodPtr = (void**)IL2CPP.GetIl2CppMethod(typePtr, IsGeneric, MethodName, returnTypeName, argTypeNames).ToPointer();
        if (methodPtr == null)
        {
            return (nint)methodPtr;
        }

        return (nint)(*methodPtr);
    }

    private static string GetFullName(Type type)
    {
        bool isPointer = type.IsPointer;
        if (isPointer)
        {
            type = type.GetElementType();
        }

        if (type.IsPrimitive || type == typeof(string))
        {
            if (isPointer) return type.MakePointerType().FullName;
            else return type.FullName;
        }
        else
        {
            var il2cppType = Il2CppType.From(type, throwOnFailure: true);
            if (isPointer) return il2cppType.MakePointerType().FullName;
            else return il2cppType.FullName;
        }
    }

    public override string ToString()
    {
        return $"{Type.FullName}.{MethodName}{(IsGeneric ? "<>" : string.Empty)}({string.Join(", ", ArgTypes.Select(arg => arg.FullName))})";
    }
}