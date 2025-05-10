using System.Linq;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.MultiTenancy;
using ABPNET.Authorization;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ABPNET.EntityFrameworkCore.Seed.Host
{
    public class HostRoleAndUserCreator
    {
        private readonly ABPNETDbContext _context;

        public HostRoleAndUserCreator(ABPNETDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            CreateHostRoleAndUsers();
        }

        private void CreateHostRoleAndUsers()
        {
            //
            // Admin role for host
            //
            var adminRoleForHost = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == null && r.Name == StaticRoleNames.Host.Admin);
            if (adminRoleForHost == null)
            {
                adminRoleForHost = _context.Roles.Add(new Role(null, StaticRoleNames.Host.Admin, StaticRoleNames.Host.Admin) { IsStatic = true, IsDefault = true }).Entity;
                _context.SaveChanges();
            }
            //
            // Grant all permissions to admin role for host
            //
            var grantedPermissions = _context.Permissions.IgnoreQueryFilters()
                .OfType<RolePermissionSetting>()
                .Where(p => p.TenantId == null && p.RoleId == adminRoleForHost.Id)
                .Select(p => p.Name)
                .ToList();

            var permissions = PermissionFinder
                .GetAllPermissions(new ABPNETAuthorizationProvider())
                .Where(p => p.MultiTenancySides.HasFlag(MultiTenancySides.Host) && !grantedPermissions.Contains(p.Name))
                .ToList();
            if (permissions.Any())
            {
                _context.Permissions.AddRange(
                    permissions.Select(permission => new RolePermissionSetting
                    {
                        TenantId = null,
                        Name = permission.Name,
                        IsGranted = true,
                        RoleId = adminRoleForHost.Id
                    })
                );
                _context.SaveChanges();
            }
            //
            // System roles
            //
            var apiClientRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == null && r.Name == StaticRoleNames.Tenants.ApiClient);
            if (apiClientRole == null)
            {
                apiClientRole = _context.Roles.Add(new Role(null, StaticRoleNames.Tenants.ApiClient, StaticRoleNames.Tenants.ApiClient) { IsStatic = true, IsDefault = true }).Entity;
                _context.SaveChanges();
            }
            var jobsRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == null && r.Name == StaticRoleNames.Tenants.Jobs);
            if (jobsRole == null)
            {
                jobsRole = _context.Roles.Add(new Role(null, StaticRoleNames.Tenants.Jobs, StaticRoleNames.Tenants.Jobs) { IsStatic = true, IsDefault = true }).Entity;
                _context.SaveChanges();
            }
            //
            // Grant permissions apiclientRole and JobsRole
            //
            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = null,
                Name = PermissionNames.Api_ClientUser,
                IsGranted = true,
                RoleId = apiClientRole.Id
            });
            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = null,
                Name = PermissionNames.Func_Jobs,
                IsGranted = true,
                RoleId = apiClientRole.Id
            });
            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = null,
                Name = PermissionNames.Func_Jobs,
                IsGranted = true,
                RoleId = jobsRole.Id
            });
            //
            // Admin user for host
            //
            var userName = AbpUserBase.AdminUserName;
            var adminUserForHost = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == null && u.UserName == userName);
            if (adminUserForHost == null)
            {
                var email = $"{userName}@{ABPNETConsts.mailService}.com";
                var adminUser = new User
                {
                    TenantId = null,
                    UserName = userName,
                    Name = userName,
                    Surname = userName,
                    EmailAddress = email,
                    IsEmailConfirmed = true,
                    IsActive = true
                };

                adminUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(adminUser, ABPNETConsts.DefaultPassword);
                adminUser.SetNormalizedNames();

                adminUserForHost = _context.Users.Add(adminUser).Entity;
                _context.SaveChanges();

                // Assign Admin role to admin user
                _context.UserRoles.Add(new UserRole(null, adminUserForHost.Id, adminRoleForHost.Id));
                _context.SaveChanges();
            }
            //
            // ApiUser for host
            //
            var apiClientName = ABPNETConsts.DefaultApiUserName;
            var apiClientUserForHost = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == null && u.UserName == ABPNETConsts.DefaultApiUserName);
            if (apiClientUserForHost == null)
            {
                var email = $"{apiClientName}@{ABPNETConsts.mailService}.com";
                var apiClientUser = new User
                {
                    TenantId = null,
                    UserName = apiClientName,
                    Name = apiClientName,
                    Surname = apiClientName,
                    EmailAddress = email,
                    IsEmailConfirmed = true,
                    IsActive = true
                };

                apiClientUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(apiClientUser, ABPNETConsts.DefaultPassword);
                apiClientUser.SetNormalizedNames();

                apiClientUserForHost = _context.Users.Add(apiClientUser).Entity;
                _context.SaveChanges();

                // Assign Admin role to api client user
                _context.UserRoles.Add(new UserRole(null, apiClientUserForHost.Id, apiClientRole.Id));
                _context.UserRoles.Add(new UserRole(null, apiClientUserForHost.Id, jobsRole.Id));
                _context.SaveChanges();
            }
        }
    }
}



