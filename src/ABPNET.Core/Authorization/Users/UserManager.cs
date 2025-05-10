using System;
using System.Collections.Generic;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using Abp.Runtime.Caching;
using ABPNET.Authorization.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ABPNET.Authorization.Users
{
    public class UserManager : AbpUserManager<Role, User>
    {
        public UserManager(
            RoleManager roleManager,
            UserStore store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<User> passwordHasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<User>> logger,
            IPermissionManager permissionManager,
            IUnitOfWorkManager unitOfWorkManager,
            ICacheManager cacheManager,
            IRepository<OrganizationUnit, long> organizationUnitRepository,
            IRepository<UserOrganizationUnit, long> userOrganizationUnitRepository,
            IOrganizationUnitSettings organizationUnitSettings,
            ISettingManager settingManager)
            : base(
                roleManager,
                store,
                optionsAccessor,
                passwordHasher,
                userValidators,
                passwordValidators,
                keyNormalizer,
                errors,
                services,
                logger,
                permissionManager,
                unitOfWorkManager,
                cacheManager,
                organizationUnitRepository,
                userOrganizationUnitRepository,
                organizationUnitSettings,
                settingManager,
                null) // TODO - add IRepository<UserLogin, long> userLoginRepository
        {
        }

        public bool IsSuperAdminCurrentUser()
        {
            return IsInRoleAsync(GetUserById(AbpSession.UserId.Value), StaticRoleNames.Tenants.SuperAdmin).Result ||
                IsInRoleAsync(GetUserById(AbpSession.UserId.Value), StaticRoleNames.Host.Admin).Result;
        }

        public bool IsAdminCurrentUser()
        {
            return IsInRoleAsync(GetUserById(AbpSession.UserId.Value), StaticRoleNames.Tenants.Admin).Result ||
                IsInRoleAsync(GetUserById(AbpSession.UserId.Value), StaticRoleNames.Host.Admin).Result;
        }
    }
}



