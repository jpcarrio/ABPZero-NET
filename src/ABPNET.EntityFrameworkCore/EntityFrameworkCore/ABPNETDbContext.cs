using Abp.Application.Features;
using Abp.Localization;
using Abp.Zero.EntityFrameworkCore;
using EntityFrameworkCore.EncryptColumn.Extension;
using EntityFrameworkCore.EncryptColumn.Interfaces;
using EntityFrameworkCore.EncryptColumn.Util;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;
//using ABPNET.Payment;
//using ABPNET.TodoList;
using ABPNET.Models;
using ABPNET.MultiTenancy;
using Microsoft.EntityFrameworkCore;
namespace ABPNET.EntityFrameworkCore
{
    public class ABPNETDbContext : AbpZeroDbContext<Tenant, Role, User, ABPNETDbContext>
    {
        /* Define a DbSet for each entity of the application */
        public DbSet<TodoTask> Tasks { get; set; }

        private readonly IEncryptionProvider _encryptionProvider;
        public ABPNETDbContext(DbContextOptions<ABPNETDbContext> options)
            : base(options)
        {
            this._encryptionProvider = new GenerateEncryptionProvider("ABPNETManager00");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Encryption of columns
            modelBuilder.UseEncryption(this._encryptionProvider);

            /* Abp related */
            modelBuilder.Entity<EditionFeatureSetting>()
                .HasOne(e => e.Edition)
                .WithMany()
                .HasForeignKey(e => e.EditionId)
                .IsRequired(false);
            /* Integer related, any integer that is smaller than 10485760 */
            modelBuilder.Entity<ApplicationLanguageText>()
                .Property(p => p.Value)
                .HasMaxLength(100);

            modelBuilder.Entity<Abp.Configuration.Setting>().Property(u => u.Value).HasMaxLength(2000000);
        }
    }
}



