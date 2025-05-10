using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Configuration;
using ISUCore.Authorization;
using ISUCore.Configuration.Dto;

namespace ISUCore.Configuration
{
    [AbpAuthorize]
    public class ConfigurationAppService : ISUCoreAppServiceBase, IConfigurationAppService
    {
        private readonly ISettingStore _settingStore;        

        public ConfigurationAppService(
            ISettingStore settingStore
        )
        {
            _settingStore = settingStore;
        }

        public async Task ChangeUiTheme(ChangeUiThemeInput input)
        {
            if (AbpSession.TenantId.Equals(null))
            {
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.UiTheme, input.Theme);
            }
            else
            {
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.UiTheme, input.Theme);
            }
        }

        [AbpAuthorize(PermissionNames.Func_ChangeApplicationSettings)]
        public async Task ChangeGlobalSettings(GlobalSettingsInput input)
        {
            if (AbpSession.TenantId.Equals(null))
            {
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppName, input.AppName);
            }
            else
            {
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.AppName, input.AppName);
            }
        }

        [AbpAuthorize(PermissionNames.Func_ChangeApplicationSettings)]
        public async Task ChangeLogo(ChangeLogoInput input)
        {
            if (AbpSession.TenantId.Equals(null))
            {
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppLogo, input.LogoContent);
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppLogoSmall, input.LogoSmall);
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppLogoName, input.LogoName);
            }
            else
            {
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.AppLogo, input.LogoContent);
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.AppLogoSmall, input.LogoSmall);
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.AppLogoName, input.LogoName);
            }
        }

        [AbpAuthorize(PermissionNames.Func_ChangeApplicationSettings)]

        public async Task SetTwoFactorEnabledGlobally(TwoFactorStatusInput input)
        {
            if (AbpSession.TenantId.Equals(null))
            {
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppEnableTwoFactor,
                    input.Active.ToString());
            }
            else
            {
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.AppEnableTwoFactor, input.Active.ToString());
            }
        }

        // Uncomment to able users set two factor authentication
        //public async Task<IdentityResult> SetTwoFactorEnabledAsync(TwoFactorStatusInput input)
        //{
        //    long userId = AbpSession.UserId.Value;
        //    var user = await UserManager.GetUserByIdAsync(userId);
        //    return await UserManager.SetTwoFactorEnabledAsync(user, input.Active);
        //}

        public async Task SetSocialMediaAuthEnabledAsync(ChangeSocialMediaAuthInput input)
        {
            if (AbpSession.TenantId.Equals(null))
            {
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppSocialMediaAuth,
                    input.IsEnabled.ToString());
            }
            else
            {
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.AppSocialMediaAuth, input.IsEnabled.ToString());
            }
        }

        [AbpAuthorize(PermissionNames.Func_ChangeShowTentant)]
        public async Task ChangeShowTentant(ChangeShowTentantInput input)
        {
            await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppShowTentant,
                input.Show.ToString());
        }

        [AbpAuthorize(PermissionNames.Func_ChangeLogoImage)]
        public async Task ChangeLogoImage(ChangeLogoImageInput input)
        {
            if (AbpSession.TenantId.Equals(null))
            {
                await SettingManager.ChangeSettingForApplicationAsync(AppSettingNames.AppChangeLogoImage,
                    input.Allowed.ToString());
            }
            else
            {
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.TenantId.GetValueOrDefault(),
                    AppSettingNames.AppChangeLogoImage, input.Allowed.ToString());
            }
        }

        [AbpAllowAnonymous]
        public bool GetShowTentant()
        {
            SettingInfo setting =
                _settingStore.GetSettingOrNullAsync(null, null, AppSettingNames.AppShowTentant).Result;
            return setting.Value.ToLower() == "true";
        }

        public async Task ChangeMaterialTheme(ChangeMaterialThemeInput input)
        {
            int? TenantId = AbpSession.TenantId.GetValueOrDefault();
            if (TenantId == 0)
                TenantId = null;

            if (!AbpSession.UserId.Equals(null))
            {
                await SettingManager.ChangeSettingForUserAsync(
                    new Abp.UserIdentifier(TenantId, AbpSession.UserId.GetValueOrDefault()),
                    AppSettingNames.AppMaterialTheme,
                    input.Classname);
            }
        }

    }
}

