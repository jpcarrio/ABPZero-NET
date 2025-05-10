using Abp.Dependency;
using Castle.Windsor.MsDependencyInjection;
using ISUCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ISUCore.Migrator.DependencyInjection
{
    public static class ServiceCollectionRegistrar
    {
        public static void Register(IIocManager iocManager)
        {
            var services = new ServiceCollection();

            IdentityRegistrar.Register(services);

            WindsorRegistrationHelper.CreateServiceProvider(iocManager.IocContainer, services);
        }
    }
}

