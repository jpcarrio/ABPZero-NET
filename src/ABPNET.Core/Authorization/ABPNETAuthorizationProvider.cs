using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;
namespace ABPNET.Authorization
{
    public class ABPNETAuthorizationProvider : AuthorizationProvider
    {
        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            context.CreatePermission(PermissionNames.Api_ClientUser, L("ApiClient"));
            context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
            context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
            context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
            context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
            context.CreatePermission(PermissionNames.Pages_Tasks, L("ToDoList"));
            context.CreatePermission(PermissionNames.Pages_Settings, L("AccountSettings"));
            context.CreatePermission(PermissionNames.Pages_EmployeesDetail, L("EmployeesDetail"));
            context.CreatePermission(PermissionNames.Pages_EmployeesList, L("EmployeesManager"));
            context.CreatePermission(PermissionNames.Pages_Imports, L("Import"));
            context.CreatePermission(PermissionNames.Pages_Projects, L("Projects"));
            context.CreatePermission(PermissionNames.Pages_Payments, L("Payments"));
            context.CreatePermission(PermissionNames.Pages_Actions, L("Actions"));
            context.CreatePermission(PermissionNames.Pages_SkillsManager, L("SkillsManager"));
            context.CreatePermission(PermissionNames.Pages_SkillsWizard, L("SkillsWizard"));
            context.CreatePermission(PermissionNames.Pages_SkillsAnalysis, L("SkillsAnalysis"));
            context.CreatePermission(PermissionNames.Pages_Reports_Projects, L("ReportsProjects"));
            context.CreatePermission(PermissionNames.Pages_Reports_TimeOffs, L("ReportsTimeOffs"));
            context.CreatePermission(PermissionNames.Pages_Reports_HumanResources, L("ReportsHumanResources"));
            context.CreatePermission(PermissionNames.Pages_Projects_Notifications, L("ProjectNotifications"));

            //Admin only
            context.CreatePermission(PermissionNames.Pages_GlobalSettings, L("GlobalSettings"));
            context.CreatePermission(PermissionNames.Func_Jobs, L("BackgroundJobs"));

            //SuperAdmin only
            context.CreatePermission(PermissionNames.Func_ChangeApplicationSettings, L("ChangeApplicationSettings"));
            context.CreatePermission(PermissionNames.Func_ChangeShowTentant, L("EnableOrDisabledTentant"), multiTenancySides: MultiTenancySides.Host);
            context.CreatePermission(PermissionNames.Func_ChangeLogoImage, L("EnableOrDisabledChangeLogoImage"), multiTenancySides: MultiTenancySides.Host);
        }
        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, ABPNETConsts.LocalizationSourceName);
        }
    }
}



