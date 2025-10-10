namespace Hikaria.Core.SNetworkExt;

public interface ICaptureCallbackObject
{
    void OnStateCapture();

    ISNetExt_Replicator GetReplicator();
}
