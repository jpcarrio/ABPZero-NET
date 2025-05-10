using Abp.Authorization;
using Abp.Domain.Uow;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;
using ISUCore.MultiTenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISUCore.Identity
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

