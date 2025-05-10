using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace ABPNET.Authorization.Roles
{
    public static class AppRoleConfig
    {
        public static void Configure(IRoleManagementConfig roleManagementConfig)
        {
            // Static host roles

            roleManagementConfig.StaticRoles.Add(
                new StaticRoleDefinition(
                    StaticRoleNames.Host.Admin,
                    MultiTenancySides.Host
                )
            );

            roleManagementConfig.StaticRoles.Add(
                new StaticRoleDefinition(
                    StaticRoleNames.Host.ApiClient,
                    MultiTenancySides.Host
                )
            );

            roleManagementConfig.StaticRoles.Add(
                new StaticRoleDefinition(
                    StaticRoleNames.Host.Jobs,
                    MultiTenancySides.Host
                )
            );

            // Static tenant roles

            roleManagementConfig.StaticRoles.Add(
                new StaticRoleDefinition(
                    StaticRoleNames.Tenants.SuperAdmin,
                    MultiTenancySides.Tenant
                )
            );

            roleManagementConfig.StaticRoles.Add(
               new StaticRoleDefinition(
                   StaticRoleNames.Tenants.Admin,
                   MultiTenancySides.Tenant
               )
           );

            roleManagementConfig.StaticRoles.Add(
               new StaticRoleDefinition(
                   StaticRoleNames.Tenants.ApiClient,
                   MultiTenancySides.Tenant
               )
           );

            roleManagementConfig.StaticRoles.Add(
                new StaticRoleDefinition(
                    StaticRoleNames.Tenants.Jobs,
                    MultiTenancySides.Tenant
                )
            );
        }
    }
}



