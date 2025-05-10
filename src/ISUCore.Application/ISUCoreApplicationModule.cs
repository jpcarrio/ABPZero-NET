using Abp.AutoMapper;
using Abp.Hangfire;
using Abp.Hangfire.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ISUCore.Authorization;

namespace ISUCore
{
    [DependsOn(
        typeof(ISUCoreCoreModule),
        typeof(AbpAutoMapperModule),
        typeof(AbpHangfireAspNetCoreModule))]
    public class ISUCoreApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.BackgroundJobs.UseHangfire();
            Configuration.Authorization.Providers.Add<ISUCoreAuthorizationProvider>();
        }

        public override void Initialize()
        {
            var thisAssembly = typeof(ISUCoreApplicationModule).GetAssembly();

            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
        }
    }
}

