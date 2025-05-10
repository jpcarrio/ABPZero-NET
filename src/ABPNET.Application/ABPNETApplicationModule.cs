using Abp.AutoMapper;
using Abp.Hangfire;
using Abp.Hangfire.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ABPNET.Authorization;

namespace ABPNET
{
    [DependsOn(
        typeof(ABPNETCoreModule),
        typeof(AbpAutoMapperModule),
        typeof(AbpHangfireAspNetCoreModule))]
    public class ABPNETApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.BackgroundJobs.UseHangfire();
            Configuration.Authorization.Providers.Add<ABPNETAuthorizationProvider>();
        }

        public override void Initialize()
        {
            var thisAssembly = typeof(ABPNETApplicationModule).GetAssembly();

            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
        }
    }
}



