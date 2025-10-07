namespace Hikaria.Core.SNetworkExt;

public class SNetExt_CaptureBuffer
{
    public SNetExt_CaptureBuffer(SNetExt_BufferType type)
    {
        this.type = type;
        m_passes = new List<byte[]>[PassCount];
        for (int i = 0; i < PassCount; i++)
        {
            m_passes[i] = new List<byte[]>();
        }
    }

    public List<byte[]> GetPass(SNetwork.eCapturePass pass)
    {
        if ((int)pass >= PassCount)
        {
            return m_passes[0];
        }
        return m_passes[(int)pass];
    }

    public void Clear()
    {
        isValid = false;
        for (int i = 0; i < PassCount; i++)
        {
            m_passes[i].Clear();
        }
    }

    public static readonly int PassCount = Enum.GetValues(typeof(SNetExt_BufferType)).Length - 1;

    public bool isValid;

    public SNetExt_BufferType type;

    public pBufferData data;

    public List<byte[]>[] m_passes;
}
