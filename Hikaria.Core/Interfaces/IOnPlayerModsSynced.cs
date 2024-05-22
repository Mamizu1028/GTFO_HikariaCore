using SNetwork;

namespace Hikaria.Core.Interfaces
{
    public interface IOnPlayerModsSynced
    {
        void OnPlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods);
    }
}
