using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Auditing;
using Abp.Runtime.Session;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;
using ISUCore.Sessions.Dto;

namespace ISUCore.Sessions
{
    public class SessionAppService : ISUCoreAppServiceBase, ISessionAppService
    {
        private readonly UserManager _userManager;
        private readonly IAbpSession _session;
        public SessionAppService(
            UserManager userManager,
            IAbpSession session)
        {
            _userManager = userManager;
            _session = session;
        }
        [DisableAuditing]
        public async Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations()
        {
            var versionHelper = new AppVersionHelper();
            var output = new GetCurrentLoginInformationsOutput
            {
                Application = new ApplicationInfoDto
                {
                    Version = versionHelper.Version,
                    ReleaseDate = versionHelper.ReleaseDate,
                    Features = new Dictionary<string, bool>()
                }
            };

            if (AbpSession.TenantId.HasValue)
            {
                output.Tenant = ObjectMapper.Map<TenantLoginInfoDto>(await GetCurrentTenantAsync());
            }

            if (AbpSession.UserId.HasValue)
            {
                var currentUser = UserManager.GetUserById(AbpSession.UserId.Value);
                if (this.IsConfirmedUser(currentUser))
                {
                    output.User = ObjectMapper.Map<UserLoginInfoDto>(await GetCurrentUserAsync());
                }
            }

            output.ImpersonatorUserId = AbpSession.ImpersonatorUserId.HasValue ? AbpSession.ImpersonatorUserId.Value : default(long?);

            output.ImpersonatorTenantId = AbpSession.ImpersonatorTenantId.HasValue ? AbpSession.ImpersonatorTenantId.Value : default(int?);

            return output;
        }
        public async Task<bool> IsAdmin(long userId)
        {
            return await Task.Run(() =>
            {
                var user = _userManager.GetUserById(_session.UserId.Value);
                return this._userManager.IsInRoleAsync(user, StaticRoleNames.Host.Admin).Result &&
                    this._userManager.IsInRoleAsync(user, StaticRoleNames.Tenants.Admin).Result;

            });
        }
        private bool IsConfirmedUser(User user)
        {
            return user.IsEmailConfirmed && string.IsNullOrEmpty(user.EmailConfirmationCode);
        }
    }
}

