using SNetwork;

namespace Hikaria.Core.Utility;

public static class Utils
{
    public static bool TryGetPlayerByCharacterSlot(int slot, out SNet_Player player)
    {
        player = null;
        int index = slot - 1;
        if (index < 0 || index > 4)
            return false;
        player = SNet.Slots.CharacterSlots[index].player;
        return player != null;
    }

    public static bool TryGetPlayerByPlayerSlot(int slot, out SNet_Player player)
    {
        player = null;
        int index = slot - 1;
        if (index < 0 || index > 4)
            return false;
        player = SNet.Slots.PlayerSlots[index].player;
        return player != null;
    }
}
