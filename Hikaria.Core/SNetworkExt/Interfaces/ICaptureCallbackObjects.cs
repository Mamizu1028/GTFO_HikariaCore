namespace Hikaria.Core.SNetworkExt;

public interface ICaptureCallbackObject
{
    void OnStateCapture();

    IReplicator GetReplicator();
}
