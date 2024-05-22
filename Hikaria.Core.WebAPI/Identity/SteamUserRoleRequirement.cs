using Hikaria.Core.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Hikaria.Core.WebAPI.Identity
{
    public class SteamUserRoleRequirement : IAuthorizationRequirement
    {
        public UserPrivilege Role { get; }

        public SteamUserRoleRequirement(UserPrivilege role)
        {
            Role = role;
        }
    }
}
