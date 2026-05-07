using System.Reflection;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_Marshaller : IDisposable
{
    public abstract void Setup(int size);

    public Type MarshalsType() => m_marshallingType;

    protected SNetExt_Marshaller()
    {
    }

    protected Type m_marshallingType;

    public abstract void Dispose();
}

public class SNetExt_Marshaller<T> : SNetExt_Marshaller where T : struct
{
    private bool m_disposed;

    ~SNetExt_Marshaller()
    {
        DisposeCore();
    }

    public override void Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    private void DisposeCore()
    {
        if (m_disposed) return;
        if (m_intPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(m_intPtr);
            m_intPtr = IntPtr.Zero;
        }
        m_disposed = true;
    }

    public bool IsBlittable { get; private set; }

    public override void Setup(int size)
    {
        Size = size;
        SizeWithIDs = Size + 33;
        m_intPtr = Marshal.AllocHGlobal(SizeWithIDs);
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
            if (f.GetCustomAttribute<MarshalAsAttribute>() != null)
                return false;
            if (!f.FieldType.IsValueType) return false;
            if (!f.FieldType.IsPrimitive && !f.FieldType.IsEnum)
            {
                if (!DetectBlittable(f.FieldType))
                    return false;
            }
        }
        return true;
    }

    public virtual void MarshalToBytes(T data, byte[] bytes)
    {
        if (m_disposed) throw new ObjectDisposedException(nameof(SNetExt_Marshaller<T>));
        if (IsBlittable)
        {
            MemoryMarshal.Write(bytes.AsSpan(33, Size), ref data);
            return;
        }
        Marshal.StructureToPtr(data, m_intPtr, true);
        Marshal.Copy(m_intPtr, bytes, 33, Size);
    }

    public void MarshalToData(byte[] bytes, ref T data)
    {
        if (m_disposed) throw new ObjectDisposedException(nameof(SNetExt_Marshaller<T>));
        if (IsBlittable)
        {
            data = MemoryMarshal.Read<T>(bytes.AsSpan(33, Size));
            return;
        }
        Marshal.Copy(bytes, 33, m_intPtr, Size);
        data = (T)Marshal.PtrToStructure(m_intPtr, m_marshallingType);
    }

    public int SizeWithIDs;

    public int Size;

    public IntPtr m_intPtr;
}

