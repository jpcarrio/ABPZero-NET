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

namespace ABPNET.EntityFrameworkCore.Seed.Tenants
{
    public class TenantRoleAndUserBuilder
    {
        private readonly ABPNETDbContext _context;
        private readonly int _tenantId;

        public TenantRoleAndUserBuilder(ABPNETDbContext context, int tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            CreateRolesAndUsers();
        }

        private void CreateRolesAndUsers()
        {

            //
            // Superadmin roles
            //
            var superadminRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.SuperAdmin);
            if (superadminRole == null)
            {
                superadminRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.SuperAdmin, StaticRoleNames.Tenants.SuperAdmin) { IsStatic = true }).Entity;
                _context.SaveChanges();
            }
            //
            // Grant all permissions to superadmin role
            //
            var grantedPermissions = _context.Permissions.IgnoreQueryFilters()
                .OfType<RolePermissionSetting>()
                .Where(p => p.TenantId == _tenantId && p.RoleId == superadminRole.Id)
                .Select(p => p.Name)
                .ToList();

            var permissions = PermissionFinder
                .GetAllPermissions(new ABPNETAuthorizationProvider())
                .Where(p => p.MultiTenancySides.HasFlag(MultiTenancySides.Tenant) && !grantedPermissions.Contains(p.Name))
                .ToList();
            if (permissions.Any())
            {
                _context.Permissions.AddRange(permissions.Select(
                    permission => new RolePermissionSetting
                    {
                        TenantId = _tenantId,
                        Name = permission.Name,
                        IsGranted = true,
                        RoleId = superadminRole.Id
                    })
                );
                _context.SaveChanges();
            }
            //
            // Admin roles
            //
            var adminRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Admin);
            if (adminRole == null)
            {
                adminRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Admin, StaticRoleNames.Tenants.Admin) { IsStatic = true, IsDefault = true }).Entity;
                _context.SaveChanges();
            }
            //
            // Grant permissions to admin role
            //
            var ignorePermissions = _context.Permissions.IgnoreQueryFilters()
                .OfType<RolePermissionSetting>()
                .Where(p =>
                p.TenantId == _tenantId &&
                (p.RoleId == superadminRole.Id) &&
                (p.Name == PermissionNames.Func_ChangeApplicationSettings || p.Name == PermissionNames.Pages_GlobalSettings))
                .Select(p => p.Name)
                .ToList();

            var allpermissions = PermissionFinder
                .GetAllPermissions(new ABPNETAuthorizationProvider())
                .Where(p => p.MultiTenancySides.HasFlag(MultiTenancySides.Tenant) && !ignorePermissions.Contains(p.Name))
                .ToList();

            if (allpermissions.Any())
            {
                _context.Permissions.AddRange(
                    allpermissions.Select(permission => new RolePermissionSetting
                    {
                        TenantId = _tenantId,
                        Name = permission.Name,
                        IsGranted = true,
                        RoleId = adminRole.Id
                    })
                );
                _context.SaveChanges();
            }
            //
            // System roles
            //
            var apiClientRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.ApiClient);
            if (apiClientRole == null)
            {
                apiClientRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.ApiClient, StaticRoleNames.Tenants.ApiClient) { IsStatic = true, IsDefault = true }).Entity;
                _context.SaveChanges();
            }
            var jobsRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Jobs);
            if (jobsRole == null)
            {
                jobsRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Jobs, StaticRoleNames.Tenants.Jobs) { IsStatic = true, IsDefault = true }).Entity;
                _context.SaveChanges();
            }
            //
            // Grant permissions apiclientRole and JobsRole
            //
            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = _tenantId,
                Name = PermissionNames.Api_ClientUser,
                IsGranted = true,
                RoleId = apiClientRole.Id
            });
            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = _tenantId,
                Name = PermissionNames.Func_Jobs,
                IsGranted = true,
                RoleId = apiClientRole.Id
            });
            _context.Permissions.Add(new RolePermissionSetting
            {
                TenantId = _tenantId,
                Name = PermissionNames.Func_Jobs,
                IsGranted = true,
                RoleId = jobsRole.Id
            });
            //
            // Admin user
            //
            var adminUser = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == _tenantId && u.UserName == AbpUserBase.AdminUserName);
            if (adminUser == null)
            {
                var email = $"{AbpUserBase.AdminUserName}{_tenantId}@{ABPNETConsts.mailService}.com";
                adminUser = User.CreateTenantAdminUser(_tenantId, email);
                adminUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(adminUser, "Freedom01!");
                adminUser.IsEmailConfirmed = true;
                adminUser.IsActive = true;

                adminUser = _context.Users.Add(adminUser).Entity;
                _context.SaveChanges();

                // Assign Admin role to admin user
                _context.UserRoles.Add(new UserRole(_tenantId, adminUser.Id, superadminRole.Id));
                _context.SaveChanges();
            }
            //
            // ApiClient user
            //
            var apiClientUser = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == _tenantId && u.UserName == ABPNETConsts.DefaultApiUserName);
            if (apiClientUser == null)
            {
                var email = $"{ABPNETConsts.DefaultApiUserName}{_tenantId}@{ABPNETConsts.mailService}.com";
                apiClientUser = User.CreateTenantUser(_tenantId, email, ABPNETConsts.DefaultApiUserName);
                apiClientUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(apiClientUser, ABPNETConsts.DefaultApiPassword);
                apiClientUser.IsEmailConfirmed = true;
                apiClientUser.IsActive = true;

                apiClientUser = _context.Users.Add(apiClientUser).Entity;
                _context.SaveChanges();

                // Assign Admin role to admin user
                _context.UserRoles.Add(new UserRole(_tenantId, apiClientUser.Id, apiClientRole.Id));
                _context.UserRoles.Add(new UserRole(_tenantId, apiClientUser.Id, jobsRole.Id));
                _context.SaveChanges();
            }

        }
    }
}



