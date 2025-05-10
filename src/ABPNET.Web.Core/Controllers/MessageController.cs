using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Domain.Uow;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;
using ABPNET.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ABPNET.Controllers
{
    [Route("api/[controller]/[action]")]
    public class MessageController : ABPNETControllerBase
    {
        private readonly ClientConfiguration _clientConfiguration;
        private readonly MailConfiguration _mailConfiguration;
        private readonly RoleManager _roleManager;
        private readonly UserManager _userManager;
        private readonly IBackgroundJobManager _backgroundJobManager;
        public MessageController(
            IOptions<ClientConfiguration> clientConfiguration,
            IOptions<MailConfiguration> mailConfiguration,
            RoleManager roleManager,
            UserManager userManager,
            IBackgroundJobManager backgroundJobManager
            )
        {
            _clientConfiguration = clientConfiguration.Value;
            _mailConfiguration = mailConfiguration.Value;
            _roleManager = roleManager;
            _userManager = userManager;
            _backgroundJobManager = backgroundJobManager;
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



