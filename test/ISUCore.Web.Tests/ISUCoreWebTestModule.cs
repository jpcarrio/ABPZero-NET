using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using ISUCore.EntityFrameworkCore;
using ISUCore.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace ISUCore.Web.Tests
{
    [DependsOn(
        typeof(ISUCoreWebMvcModule),
        typeof(AbpAspNetCoreTestBaseModule)
    )]
    public class ISUCoreWebTestModule : AbpModule
    {
        public ISUCoreWebTestModule(ISUCoreEntityFrameworkModule abpProjectNameEntityFrameworkModule)
        {
            abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
        } 
        
        public override void PreInitialize()
        {
            Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(ISUCoreWebTestModule).GetAssembly());
        }
        
        public override void PostInitialize()
        {
            IocManager.Resolve<ApplicationPartManager>()
                .AddApplicationPartsIfNotAddedBefore(typeof(ISUCoreWebMvcModule).Assembly);
        }
    }
}
