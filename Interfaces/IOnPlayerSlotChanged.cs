using SNetwork;

namespace Hikaria.Core.Interfaces;

public interface IOnPlayerSlotChanged
{
    void OnPlayerSlotChanged(SNet_Player player, SNet_SlotType type, SNet_SlotHandleType handle, int index);
}
