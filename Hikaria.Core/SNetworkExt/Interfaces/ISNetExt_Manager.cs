namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_Manager
{
    void Setup();

    void SetupReplication();

    void OnResetSession();

    void OnValidateMasterData();
}
