using GTFO.API;
using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_PacketFreeSize<T> : SNetExt_Packet where T : struct
{
    private Action<ulong, T> ValidateAction { get; set; }
    private Action<ulong, T> ReceiveAction { get; set; }

    public static SNetExt_PacketFreeSize<T> Create(string eventName, Action<ulong, T> receiveAction, Action<ulong, T> validateAction = null)
    {
        int size = Marshal.SizeOf<T>();
        if (size >= MAX_BYTES_LENGTH)
        {
            throw new ArgumentException($"PacketData Exceed size of {MAX_BYTES_LENGTH} : Unable to Serialize", "T");
        }
        var packet = new SNetExt_PacketFreeSize<T>
        {
            EventName = eventName,
            ReceiveAction = receiveAction,
            ValidateAction = validateAction
        };
        NetworkAPI.RegisterFreeSizedEvent(eventName, packet.OnReceiveBytes);
        return packet;
    }

    public void Send(T data, SNetwork.SNet_ChannelType type, SNetwork.SNet_Player player = null)
    {
        StructureToInternalBytes(data);
        if (player == null)
        {
            NetworkAPI.InvokeFreeSizedEvent(EventName, m_internalBytes, type);
        }
        else
        {
            NetworkAPI.InvokeFreeSizedEvent(EventName, m_internalBytes, player, type);
        }
        OnReceiveBytes(SNetwork.SNet.LocalPlayer.Lookup, m_internalBytes);
    }

    public void OnReceiveBytes(ulong sender, byte[] bytes)
    {
        ByteArrayToStructure(bytes);
        if (SNetwork.SNet.IsMaster && ValidateAction != null)
        {
            ValidateAction(sender, m_data);
            return;
        }
        ReceiveAction(sender, m_data);
    }

    private void StructureToInternalBytes(T data)
    {
        int size = Marshal.SizeOf(data);
        if (size >= MAX_BYTES_LENGTH)
        {
            throw new ArgumentException($"PacketData Exceed size of {MAX_BYTES_LENGTH} : Unable to Serialize", "T");
        }
        byte[] bytes = new byte[MAX_BYTES_LENGTH];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(data, ptr, false);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);
        m_internalBytes = bytes;
    }

    private void ByteArrayToStructure(byte[] bytes)
    {
        int size = Marshal.SizeOf(typeof(T));
        if (size > bytes.Length)
        {
            throw new ArgumentException($"Packet Exceed size of {MAX_BYTES_LENGTH} : Unable to Deserialize", "T");
        }
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(bytes, 0, ptr, size);
        m_data = (T)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);
    }

    private T m_data;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_BYTES_LENGTH)]
    private byte[] m_internalBytes;

    private const int MAX_BYTES_LENGTH = 10240;
}
