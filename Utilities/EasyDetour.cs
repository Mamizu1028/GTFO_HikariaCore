using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using System.Text;
using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.Utilities;

public static class EasyDetour
{
    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateArSubLoggerInstance(nameof(EasyDetour));
    public static bool TryCreate<T>(DetourDescriptor descriptor, T to, out T originalCall, out INativeDetour detourInstance) where T : Delegate
    {
        try
        {
            IntPtr methodPointer = descriptor.GetMethodPointer();
            detourInstance = INativeDetour.CreateAndApply<T>(methodPointer, to, out originalCall);
            bool result = detourInstance != null;
            if (result)
            {
                Logger.Success($"NativeDetour Apply Success: \n{descriptor.GetDetailInfo()}");
            }
            else
            {
                Logger.Fail($"NativeDetour Apply Failed: \n{descriptor.GetDetailInfo()}");
            }
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception Thrown while creating Detour:");
            Logger.Exception(ex);
        }
        originalCall = default(T);
        detourInstance = null;
        return false;
    }

    public unsafe delegate void StaticVoidDelegate(Il2CppMethodInfo* methodInfo);

    public unsafe delegate void InstanceVoidDelegate(IntPtr instancePtr, Il2CppMethodInfo* methodInfo);
}

public struct DetourDescriptor
{
    public unsafe IntPtr GetMethodPointer()
    {
        if (Type == null)
        {
            throw new MissingFieldException("Field Type is not set!");
        }
        if (ReturnType == null)
        {
            throw new MissingFieldException("Field ReturnType is not set! If you mean 'void' do typeof(void)");
        }
        if (string.IsNullOrEmpty(MethodName))
        {
            throw new MissingFieldException("Field MethodName is not set or valid!");
        }
        Il2CppType.From(Type, true);
        IntPtr nativeClassPointer = Il2CppClassPointerStore.GetNativeClassPointer(Type);
        string fullName = GetFullName(ReturnType);
        string[] array;
        if (ArgTypes == null || ArgTypes.Length == 0)
        {
            array = Array.Empty<string>();
        }
        else
        {
            int num = ArgTypes.Length;
            array = new string[num];
            for (int i = 0; i < num; i++)
            {
                Type type = ArgTypes[i];
                array[i] = GetFullName(type);
            }
        }
        void** ptr = (void**)IL2CPP.GetIl2CppMethod(nativeClassPointer, IsGeneric, MethodName, fullName, array).ToPointer();
        if (ptr == null)
        {
            return (IntPtr)ptr;
        }
        return *(IntPtr*)ptr;
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
            if (isPointer)
            {
                return type.MakePointerType().FullName;
            }
            return type.FullName;
        }
        else
        {
            var type2 = Il2CppType.From(type, true);
            if (isPointer)
            {
                return type2.MakePointerType().FullName;
            }
            return type2.FullName;
        }
    }

    public string GetDetailInfo()
    {
        StringBuilder sb = new();
        sb.AppendLine($"Type: {Type.FullName}");
        sb.AppendLine($"MethodName: {MethodName}");
        sb.AppendLine($"ArgTypes: {string.Join(", ", ArgTypes.Select(p => p.FullName))}");
        sb.Append($"IsGeneric: {IsGeneric}");
        return sb.ToString();
    }

    public Type Type;

    public Type ReturnType;

    public Type[] ArgTypes;

    public string MethodName;

    public bool IsGeneric;
}
