using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace ABPNET.EntityFrameworkCore
{
    public static class ABPNETDbContextConfigurer
    {
        public static void Configure(DbContextOptionsBuilder<ABPNETDbContext> builder, string connectionString)
        {
            builder.UseSqlServer(connectionString);
        }

        public static void Configure(DbContextOptionsBuilder<ABPNETDbContext> builder, DbConnection connection)
        {
            builder.UseSqlServer(connection);
        }
    }
}



