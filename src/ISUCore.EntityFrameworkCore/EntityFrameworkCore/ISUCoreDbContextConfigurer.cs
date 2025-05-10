using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace ISUCore.EntityFrameworkCore
{
    public static class ISUCoreDbContextConfigurer
    {
        public static void Configure(DbContextOptionsBuilder<ISUCoreDbContext> builder, string connectionString)
        {
            builder.UseSqlServer(connectionString);
        }

        public static void Configure(DbContextOptionsBuilder<ISUCoreDbContext> builder, DbConnection connection)
        {
            builder.UseSqlServer(connection);
        }
    }
}

