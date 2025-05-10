using Abp.Hangfire;
using Abp.Hangfire.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ABPNET.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ABPNET.Web.Host.Startup
{
    [DependsOn(
       typeof(ABPNETWebCoreModule),
        typeof(AbpHangfireAspNetCoreModule))]
    public class ABPNETWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;
        public ABPNETWebHostModule(IWebHostEnvironment env)
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
            IocManager.RegisterAssemblyByConvention(typeof(ABPNETWebHostModule).GetAssembly());
        }
    }
}



