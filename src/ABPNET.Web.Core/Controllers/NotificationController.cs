using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABPNET.Controllers
{
    [Route("api/[controller]/[action]")]
    public class NotificationController : ABPNETControllerBase
    {
        private readonly RoleManager _roleManager;
        private readonly UserManager _userManager;

        public NotificationController(
            RoleManager roleManager,
            UserManager userManager
            )
        {
            this._roleManager = roleManager;
            this._userManager = userManager;

        }

        #region Email & Util
        private User ResolveAuthenticatedUser()
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var userId = this.AbpSession.UserId;
                var currentUser = _userManager.GetUserById((long)userId);
                return currentUser;
            }
        }
        /// <summary>
        /// Get all userIds by role
        /// </summary>
        /// <param name="staticRoleName">filter by role, see ref: \Authorization\Roles\StaticRoleNames.cs</param>
        /// <returns>list of users</returns>
        private async Task<List<User>> GetUsersByRoleAsync(string[] staticRoleNames)
        {
            var roleGlobal = await _roleManager.Roles.FirstOrDefaultAsync(t => staticRoleNames.Contains(t.Name));
            return await _userManager.Users.Where(u => u.Roles.Any(r => roleGlobal != null && r.RoleId == roleGlobal.Id && r.UserId == u.Id)).ToListAsync();
        }        
        #endregion
    }
}



