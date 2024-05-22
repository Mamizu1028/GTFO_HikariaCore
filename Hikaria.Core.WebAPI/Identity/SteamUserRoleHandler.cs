using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Hikaria.Core.WebAPI.Identity
{
    public class SteamUserRoleHandler : AuthorizationHandler<SteamUserRoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SteamUserRoleRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == requirement.Role.ToString()))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
