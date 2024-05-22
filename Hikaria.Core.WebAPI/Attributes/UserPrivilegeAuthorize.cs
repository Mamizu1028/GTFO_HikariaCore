using Hikaria.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace Hikaria.Core.WebAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class UserPrivilegeAuthorize : AuthorizeAttribute
    {
        public UserPrivilegeAuthorize(UserPrivilege permissions = UserPrivilege.None) : base()
        {
            Roles = UserPermissionsToRoleString(permissions);
        }

        public static string UserPermissionsToRoleString(UserPrivilege enumValue)
        {
            var values = Enum.GetValues<UserPrivilege>();
            StringBuilder sb = new();
            foreach (var value in values)
            {
                if (enumValue.HasFlag(value))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(value.ToString());
                }
            }
            return sb.ToString();
        }
    }
}
