using System.Collections.Generic;
using Abp.Configuration;

namespace ABPNET.Configuration
{
    public class AppSettingProvider : SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                new SettingDefinition(AppSettingNames.UiTheme, "sea", scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppName, "ABPNET", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppLogo, "Logo", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppLogoSmall, "LogoSmall", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppLogoName, "ABPNET Logo", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppSocialMediaAuth, "false", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppEnableTwoFactor, "false", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppShowTentant, "true", scopes: SettingScopes.Application, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppChangeLogoImage, "true", scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(Abp.Zero.Configuration.AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin, "true", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),                
                //Jobs - Birthday
                new SettingDefinition(AppSettingNames.AppNotifyBirthday, string.Empty, scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppNotifyToBirthday,"Jobs" , scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                //Jobs - Import
                new SettingDefinition(AppSettingNames.AppImportDaysOfWeek, string.Empty, scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppImportHour, string.Empty, scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppImportIsActive,"false", scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppImportLastImported, string.Empty, scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                
                // Jobs - Reminders
                new SettingDefinition(AppSettingNames.AppReminderEmployeeWeeklyHoursIsActive, string.Empty, scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppReminderEmployeeMonthlyHoursIsActive, string.Empty, scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppReminderRoles, string.Empty, scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppNotifyToEmployeeReminder, string.Empty , scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),

                // Material Theme
                new SettingDefinition(AppSettingNames.AppMaterialTheme, "", scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User, isVisibleToClients: true),
                //Jobs - Project Notifications
                new SettingDefinition(AppSettingNames.AppProjectNotificationsEmployees, string.Empty , scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
                new SettingDefinition(AppSettingNames.AppProjectNotificationsDaysBefore, string.Empty , scopes: SettingScopes.Application | SettingScopes.Tenant, isVisibleToClients: true),
            };
        }
    }
}



