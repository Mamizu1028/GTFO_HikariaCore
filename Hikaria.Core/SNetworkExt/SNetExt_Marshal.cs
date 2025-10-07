using System.Runtime.InteropServices;

namespace Hikaria.Core.SNetworkExt;

public static class SNetExt_Marshal
{
    public static SNetExt_Marshaller<T> GetMarshaler<T>() where T : struct
    {
        var mashaller = new SNetExt_Marshaller<T>();
        mashaller.Setup(Marshal.SizeOf<T>());
        return mashaller;
    }
}
