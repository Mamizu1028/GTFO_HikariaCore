using System.Reflection;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_Marshaller
{
    public abstract void Setup(int size);

    public Type MarshalsType() => m_marshallingType;

    protected SNetExt_Marshaller() { }

    protected Type m_marshallingType;
}

public class SNetExt_Marshaller<T> : SNetExt_Marshaller where T : struct
{
    public bool IsBlittable { get; private set; }

    public int SizeWithIDs;

    public int Size;

    public override void Setup(int size)
    {
        Size = size;
        SizeWithIDs = Size + 33;
        m_marshallingType = typeof(T);
        IsBlittable = DetectBlittable(typeof(T));
    }

    private static bool DetectBlittable(Type type)
    {
        if (type.IsPrimitive) return true;
        if (type.IsEnum) return true;
        if (type == typeof(string)) return false;
        if (!type.IsValueType) return false;
        if (type == typeof(IntPtr) || type == typeof(UIntPtr)) return true;
        if (type == typeof(decimal)) return false;

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var f in fields)
        {
            if (f.GetCustomAttribute<MarshalAsAttribute>() != null) return false;
            if (!f.FieldType.IsValueType) return false;
            if (!f.FieldType.IsPrimitive && !f.FieldType.IsEnum)
                if (!DetectBlittable(f.FieldType)) return false;
        }
        return true;
    }

    public virtual void MarshalToBytes(T data, byte[] bytes)
    {
        if (IsBlittable)
        {
            MemoryMarshal.Write(bytes.AsSpan(33, Size), ref data);
            return;
        }
        IntPtr ptr = Marshal.AllocHGlobal(Size);
        try
        {
            Marshal.StructureToPtr(data, ptr, false);
            Marshal.Copy(ptr, bytes, 33, Size);
        }
        finally
        {
            Marshal.DestroyStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
        }
    }

    public void MarshalToData(byte[] bytes, ref T data)
    {
        if (IsBlittable)
        {
            data = MemoryMarshal.Read<T>(bytes.AsSpan(33, Size));
            return;
        }
        IntPtr ptr = Marshal.AllocHGlobal(Size);
        try
        {
            Marshal.Copy(bytes, 33, ptr, Size);
            data = (T)Marshal.PtrToStructure(ptr, m_marshallingType);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
