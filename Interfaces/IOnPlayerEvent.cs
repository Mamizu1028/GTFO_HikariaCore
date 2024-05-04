using SNetwork;

namespace Hikaria.Core.Interfaces;

public interface IOnPlayerEvent
{
    void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason);
}
