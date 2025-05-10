using ABPNET.Configuration;
using ABPNET.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ABPNET.EntityFrameworkCore
{
    /* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
    public class ABPNETDbContextFactory : IDesignTimeDbContextFactory<ABPNETDbContext>
    {
        public ABPNETDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ABPNETDbContext>();
            var configuration = AppConfigurations.Get(WebContentDirectoryFinder.CalculateContentRootFolder());

            ABPNETDbContextConfigurer.Configure(builder, configuration.GetConnectionString(ABPNETConsts.ConnectionStringName));

            return new ABPNETDbContext(builder.Options);
        }
    }
}



