using SNetwork;

namespace Hikaria.Core.SNetworkExt;

public interface IReplicatedPlayerData
{
    public SNetStructs.pPlayer PlayerData { get; set; }
}
