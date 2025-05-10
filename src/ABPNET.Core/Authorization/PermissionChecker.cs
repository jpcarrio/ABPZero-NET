using Abp.Authorization;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;

namespace ABPNET.Authorization
{
    public class PermissionChecker : PermissionChecker<Role, User>
    {
        public PermissionChecker(UserManager userManager)
            : base(userManager)
        {
        }
    }
}



