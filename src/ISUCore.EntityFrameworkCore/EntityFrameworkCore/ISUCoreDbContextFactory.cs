using ISUCore.Configuration;
using ISUCore.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ISUCore.EntityFrameworkCore
{
    /* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
    public class ISUCoreDbContextFactory : IDesignTimeDbContextFactory<ISUCoreDbContext>
    {
        public ISUCoreDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ISUCoreDbContext>();
            var configuration = AppConfigurations.Get(WebContentDirectoryFinder.CalculateContentRootFolder());

            ISUCoreDbContextConfigurer.Configure(builder, configuration.GetConnectionString(ISUCoreConsts.ConnectionStringName));

            return new ISUCoreDbContext(builder.Options);
        }
    }
}

