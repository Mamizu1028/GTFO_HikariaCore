using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public abstract class SNetExt_Marshaller
{
    public abstract void Setup(int size);

    public Type MarshalsType() => m_marshallingType;

    protected SNetExt_Marshaller()
    {
    }

    protected Type m_marshallingType;
}

public class SNetExt_Marshaller<T> : SNetExt_Marshaller where T : struct
{
    ~SNetExt_Marshaller()
    {
        Marshal.FreeHGlobal(m_intPtr);
    }

    public override void Setup(int size)
    {
        Size = size;
        SizeWithIDs = Size + 33;
        m_intPtr = Marshal.AllocHGlobal(SizeWithIDs);
        m_marshallingType = typeof(T);
    }

    public virtual void MarshalToBytes(T data, byte[] bytes)
    {
        Marshal.StructureToPtr(data, m_intPtr, true);
        Marshal.Copy(m_intPtr, bytes, 33, Size);
    }

    public void MarshalToData(byte[] bytes, ref T data)
    {
        Marshal.Copy(bytes, 33, m_intPtr, Size);
        data = (T)Marshal.PtrToStructure(m_intPtr, m_marshallingType);
    }

    public int SizeWithIDs;

    public int Size;

    public IntPtr m_intPtr;
}