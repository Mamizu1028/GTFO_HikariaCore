using Hikaria.Core.Extensions;
using Player;
using SNetwork;

namespace Hikaria.Core.Utilities
{
    public static class SharedUtils
    {
        public static bool TryGetPlayerByCharacterIndex(int index, out SNet_Player player)
        {
            player = SNet.LobbyPlayers.ToSystemList().FirstOrDefault(p => p.CharacterSlot.index == index);
            return player != null;
        }

        public static bool TryGetPlayerBySlot(int slot, out SNet_Player player)
        {
            player = null;
            int index = slot - 1;
            if (index < 0 || index > 4)
                return false;
            if (!PlayerManager.TryGetPlayerAgent(ref index, out PlayerAgent playerAgent))
                return false;
            player = playerAgent.Owner;
            return true;
        }
    }
}
