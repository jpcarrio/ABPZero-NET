using Abp.Authorization;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;

namespace ISUCore.Authorization
{
    public class PermissionChecker : PermissionChecker<Role, User>
    {
        public PermissionChecker(UserManager userManager)
            : base(userManager)
        {
        }
    }
}

