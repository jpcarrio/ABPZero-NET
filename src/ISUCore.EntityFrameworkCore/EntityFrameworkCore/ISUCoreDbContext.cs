using Abp.Application.Features;
using Abp.Localization;
using Abp.Zero.EntityFrameworkCore;
using EntityFrameworkCore.EncryptColumn.Extension;
using EntityFrameworkCore.EncryptColumn.Interfaces;
using EntityFrameworkCore.EncryptColumn.Util;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;
//using ISUCore.Payment;
//using ISUCore.TodoList;
using ISUCore.Models;
using ISUCore.MultiTenancy;
using Microsoft.EntityFrameworkCore;
namespace ISUCore.EntityFrameworkCore
{
    public class ISUCoreDbContext : AbpZeroDbContext<Tenant, Role, User, ISUCoreDbContext>
    {
        /* Define a DbSet for each entity of the application */
        public DbSet<TodoTask> Tasks { get; set; }

        private readonly IEncryptionProvider _encryptionProvider;
        public ISUCoreDbContext(DbContextOptions<ISUCoreDbContext> options)
            : base(options)
        {
            this._encryptionProvider = new GenerateEncryptionProvider("ISUCoreManager00");
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

