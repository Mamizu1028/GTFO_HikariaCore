using SNetwork;

namespace Hikaria.Core.Interfaces;

public interface IOnSessionMemberChanged
{
    void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent);
}

public enum SessionMemberEvent
{
    JoinSessionHub,
    LeftSessionHub
}
