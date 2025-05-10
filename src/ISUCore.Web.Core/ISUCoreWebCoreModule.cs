using System;
using System.Text;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.SignalR;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Zero.Configuration;
using ISUCore.Authentication.JwtBearer;
using ISUCore.Configuration;
using ISUCore.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
namespace ISUCore
{
    [DependsOn(
         typeof(ISUCoreApplicationModule),
         typeof(ISUCoreEntityFrameworkModule),
         typeof(AbpAspNetCoreModule),
         typeof(AbpAspNetCoreSignalRModule)
     )]
    public class ISUCoreWebCoreModule : AbpModule
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfigurationRoot _appConfiguration;
        public ISUCoreWebCoreModule(IWebHostEnvironment env)
        {
            _hostingEnvironment = env;
            _appConfiguration = env.GetAppConfiguration();
        }
        public override void PreInitialize()
        {
            if (_hostingEnvironment.IsDevelopment())
            {
                Configuration.Modules.AbpWebCommon().SendAllExceptionsToClients = true;
            }
            Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
                ISUCoreConsts.ConnectionStringName
            );
            // Use database for language management
            Configuration.Modules.Zero().LanguageManagement.EnableDbLocalization();
            Configuration.Modules.AbpAspNetCore()
                 .CreateControllersForAppServices(
                     typeof(ISUCoreApplicationModule).GetAssembly()
                 );
            ConfigureTokenAuth();
        }
        private void ConfigureTokenAuth()
        {
            IocManager.Register<TokenAuthConfiguration>();
            var tokenAuthConfig = IocManager.Resolve<TokenAuthConfiguration>();
            tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appConfiguration["Authentication:JwtBearer:SecurityKey"]));
            tokenAuthConfig.Issuer = _appConfiguration["Authentication:JwtBearer:Issuer"];
            tokenAuthConfig.Audience = _appConfiguration["Authentication:JwtBearer:Audience"];
            tokenAuthConfig.SigningCredentials = new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
            tokenAuthConfig.Expiration = TimeSpan.FromDays(1);
        }
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(ISUCoreWebCoreModule).GetAssembly());
        }
        public override void PostInitialize()
        {
            IocManager.Resolve<ApplicationPartManager>()
                .AddApplicationPartsIfNotAddedBefore(typeof(ISUCoreWebCoreModule).Assembly);
        }
    }
}

