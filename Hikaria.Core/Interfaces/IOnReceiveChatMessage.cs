using SNetwork;

namespace Hikaria.Core.Interfaces
{
    public interface IOnReceiveChatMessage
    {
        void OnReceiveChatMessage(SNet_Player player, string message);
    }
}
