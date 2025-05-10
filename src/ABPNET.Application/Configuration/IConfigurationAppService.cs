using System.Threading.Tasks;
using ABPNET.Configuration.Dto;

namespace ABPNET.Configuration
{
    public interface IConfigurationAppService
    {
        Task ChangeUiTheme(ChangeUiThemeInput input);

        // Uncomment to able users set two factor authentication
        //Task<IdentityResult> SetTwoFactorEnabledAsync(TwoFactorStatusInput input);
        Task SetTwoFactorEnabledGlobally(TwoFactorStatusInput input);
        Task SetSocialMediaAuthEnabledAsync(ChangeSocialMediaAuthInput input);
        Task ChangeShowTentant(ChangeShowTentantInput input);
        Task ChangeLogoImage(ChangeLogoImageInput input);
        bool GetShowTentant();
        Task ChangeMaterialTheme(ChangeMaterialThemeInput input);
    }
}



