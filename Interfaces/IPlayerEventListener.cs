using SNetwork;

namespace Hikaria.Core.Interfaces
{
    public interface IPlayerEventListener
    {
        void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason);
    }
}
