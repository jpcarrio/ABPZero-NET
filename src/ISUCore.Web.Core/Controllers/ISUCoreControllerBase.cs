using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using ISUCore.Configuration;
using ISUCore.Helpers;
using ISUCore.Models.Message;
using Microsoft.AspNetCore.Identity;
namespace ISUCore.Controllers
{
    public abstract class ISUCoreControllerBase : AbpController
    {
        protected ISUCoreControllerBase()
        {
            LocalizationSourceName = ISUCoreConsts.LocalizationSourceName;
        }
        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
        protected async Task<EmailModel> GetAppMailMessage(string viewName, EmailModel emailModel)
        {
            try
            {
                // append settings data
                var appLogoSmall = await this.SettingManager.GetSettingValueAsync(AppSettingNames.AppLogoSmall);
                emailModel.AppLogo = appLogoSmall;
                var appName = await this.SettingManager.GetSettingValueAsync(AppSettingNames.AppName);
                emailModel.AppName = appName;
                //
                // render email view
                //
                emailModel.Html = this.RenderView(viewName, emailModel, false);
                return emailModel;
            }
            catch (Exception ex)
            {
                Abp.Logging.LogHelper.LogException(ex);
                return null;
            }
        }
    }
}

