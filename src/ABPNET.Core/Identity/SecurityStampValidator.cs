using Abp.Authorization;
using Abp.Domain.Uow;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;
using ABPNET.MultiTenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ABPNET.Identity
{
    public class SecurityStampValidator : AbpSecurityStampValidator<Tenant, Role, User>
    {
        public SecurityStampValidator(
            IOptions<SecurityStampValidatorOptions> options,
            SignInManager signInManager,
            ISystemClock systemClock,
            ILoggerFactory loggerFactory,
            IUnitOfWorkManager unitOfWorkManager)
            : base(
                  options,
                  signInManager,
                  systemClock,
                  loggerFactory,
                  unitOfWorkManager)
        {
        }
    }
}



