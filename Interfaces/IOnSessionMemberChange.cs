using SNetwork;

namespace Hikaria.Core.Interfaces
{
    public interface IOnSessionMemberChange
    {
        void OnSessionMemberChange(SNet_Player player, SessionMemberEvent sessionMemberEvent);
    }
}
