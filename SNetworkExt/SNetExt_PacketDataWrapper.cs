using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_PacketDataWrapper<T> : ISNetExt_PacketDataWrapper where T : struct
{
    public void ClearData()
    {
        m_datas.Clear();
    }

    public void AddData(T data)
    {
        m_datas.Add(data);
    }

    public List<T> GetDataList()
    {
        return m_datas;
    }

    public int GetTotalByteSize()
    {
        return GetByteSize() * m_datas.Count;
    }

    public int GetCount()
    {
        return m_datas.Count;
    }

    private int GetByteSize()
    {
        return Marshal.SizeOf(new T());
    }

    public void Serialize(byte[] bytes, ref int byteIndexOffset)
    {
        int byteSize = GetByteSize();
        T[] array = m_datas.ToArray();
        int num = array.Length;
        s_tempIntPtr = Marshal.AllocHGlobal(byteSize);
        for (int i = 0; i < num; i++)
        {
            Marshal.StructureToPtr(array[i], s_tempIntPtr, true);
            Marshal.Copy(s_tempIntPtr, bytes, byteIndexOffset + i * byteSize, byteSize);
        }
        byteIndexOffset += byteSize * num;
    }

    public void Deserialize(int objCount, byte[] bytes, ref int byteIndexOffset)
    {
        int byteSize = GetByteSize();
        s_tempIntPtr = Marshal.AllocHGlobal(byteSize);
        m_datas.Clear();
        for (int i = 0; i < objCount; i++)
        {
            Marshal.Copy(bytes, byteIndexOffset + i * byteSize, s_tempIntPtr, byteSize);
            T t = (T)Marshal.PtrToStructure(s_tempIntPtr, typeof(T));
            m_datas.Add(t);
        }
        Marshal.FreeHGlobal(s_tempIntPtr);
    }

    private List<T> m_datas = new List<T>();

    private static IntPtr s_tempIntPtr;
}
