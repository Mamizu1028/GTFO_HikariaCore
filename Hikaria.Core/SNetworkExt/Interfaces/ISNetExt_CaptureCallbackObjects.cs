namespace Hikaria.Core.SNetworkExt;

public interface ICaptureCallbackObject
{
    void OnStateCapture();

    bool PersistAcrossSession { get; }
}
