using SNetwork;

namespace Hikaria.Core.Interfaces;

public interface IOnMasterCommand
{
    void OnMasterCommand(eMasterCommandType type, int refA);
}
