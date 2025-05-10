using Abp.Hangfire;
using Abp.Hangfire.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ISUCore.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ISUCore.Web.Host.Startup
{
    [DependsOn(
       typeof(ISUCoreWebCoreModule),
        typeof(AbpHangfireAspNetCoreModule))]
    public class ISUCoreWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;
        public ISUCoreWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }
        public override void PreInitialize()
        {
            Configuration.BackgroundJobs.UseHangfire();
        }
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(ISUCoreWebHostModule).GetAssembly());
        }
    }
}

