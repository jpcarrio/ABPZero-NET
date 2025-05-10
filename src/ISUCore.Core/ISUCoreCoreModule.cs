using Abp.Configuration;
using Abp.Localization;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Timing;
using Abp.Zero;
using Abp.Zero.Configuration;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;
using ISUCore.Configuration;
using ISUCore.Localization;
using ISUCore.MultiTenancy;
using ISUCore.Timing;

namespace ISUCore
{
    [DependsOn(typeof(AbpZeroCoreModule))]
    public class ISUCoreCoreModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Auditing.IsEnabledForAnonymousUsers = true;

            // Declare entity types
            Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
            Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
            Configuration.Modules.Zero().EntityTypes.User = typeof(User);

            ISUCoreLocalizationConfigurer.Configure(Configuration.Localization);

            // TODO: turn nginx ignore headers on, and test again changing name.
            // ref: https://stackoverflow.com/questions/49783689/cannot-get-header-that-contains-a-period-from-nginx
            // IMPORTANT: request header without dot, change 'Abp.TenantId' for 'Abp-TenantId'
            // Configuration.MultiTenancy.TenantIdResolveKey = "Abp-TenantId";

            // Configure roles
            AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

            Configuration.Settings.Providers.Add<AppSettingProvider>();

            Configuration.Localization.Languages.Add(new LanguageInfo("fa", "فارسی", "famfamfam-flags ir"));
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(ISUCoreCoreModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;
            // Enable this line to create a multi-tenant application.
            try
            {
                // Get App.ShowTentant setting with SettingScopes Application
                //bool multiTenancyEnabled = IocManager.Resolve<SettingStore>().GetSettingOrNull(null, null, AppSettingNames.AppShowTentant).Value.ToLower() == "true";
                Configuration.MultiTenancy.IsEnabled = true; //multiTenancyEnabled;
            }
            catch (System.Exception)
            {
                // If fail on Migrations because database not exists set a constant value
                Configuration.MultiTenancy.IsEnabled = ISUCoreConsts.MultiTenancyEnabled;
            }

        }
    }
}

