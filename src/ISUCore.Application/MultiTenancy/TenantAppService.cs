using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.MultiTenancy;
using Abp.Runtime.Security;
using ISUCore.Authorization;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;
using ISUCore.Configuration;
using ISUCore.Editions;
using ISUCore.MultiTenancy.Dto;
using Microsoft.AspNetCore.Identity;

namespace ISUCore.MultiTenancy
{
    [AbpAuthorize(PermissionNames.Pages_Tenants)]
    public class TenantAppService : AsyncCrudAppService<Tenant, TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>, ITenantAppService
    {
        private readonly TenantManager _tenantManager;
        private readonly EditionManager _editionManager;
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IAbpZeroDbMigrator _abpZeroDbMigrator;
        private readonly ISettingStore _settingStore;

        public TenantAppService(
            IRepository<Tenant, int> repository,
            TenantManager tenantManager,
            EditionManager editionManager,
            UserManager userManager,
            RoleManager roleManager,
            IAbpZeroDbMigrator abpZeroDbMigrator,
            ISettingStore settingStore)
            : base(repository)
        {
            _tenantManager = tenantManager;
            _editionManager = editionManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _abpZeroDbMigrator = abpZeroDbMigrator;
            _settingStore = settingStore;
        }

        public override async Task<TenantDto> CreateAsync(CreateTenantDto input)
        {
            CheckCreatePermission();

            // Create tenant
            var tenant = ObjectMapper.Map<Tenant>(input);
            tenant.ConnectionString = input.ConnectionString.IsNullOrEmpty()
                ? null
                : SimpleStringCipher.Instance.Encrypt(input.ConnectionString);

            var defaultEdition = await _editionManager.FindByNameAsync(EditionManager.DefaultEditionName);
            if (defaultEdition != null)
            {
                tenant.EditionId = defaultEdition.Id;
            }

            await _tenantManager.CreateAsync(tenant);
            await CurrentUnitOfWork.SaveChangesAsync(); // To get new tenant's id.
            var tenantDto = MapToEntityDto(tenant);

            // Create tenant database
            _abpZeroDbMigrator.CreateOrMigrateForTenant(tenant);

            // We are working entities of new tenant, so changing tenant filter
            using (CurrentUnitOfWork.SetTenantId(tenant.Id))
            {
                // Create static roles for new tenant
                CheckErrors(await _roleManager.CreateStaticRoles(tenant.Id));

                await CurrentUnitOfWork.SaveChangesAsync(); // To get static role ids

                // Grant all permissions to admin role
                var adminRole = _roleManager.Roles.Single(r => r.Name == StaticRoleNames.Tenants.SuperAdmin);
                await _roleManager.GrantAllPermissionsAsync(adminRole);

                // Create admin user for the tenant
                var adminUser = User.CreateTenantAdminUser(tenant.Id, input.AdminEmailAddress);
                adminUser.IsEmailConfirmed = true;
                await _userManager.InitializeOptionsAsync(tenant.Id);
                CheckErrors(await _userManager.CreateAsync(adminUser, User.DefaultPassword));
                await CurrentUnitOfWork.SaveChangesAsync(); // To get admin user's id

                // Assign admin user to role!
                CheckErrors(await _userManager.AddToRoleAsync(adminUser, adminRole.Name));
                await CurrentUnitOfWork.SaveChangesAsync();

                // Return new Admin UserId
                tenantDto.AdminUserId = adminUser.Id;
            }

            await AddSettingIfNotExists(AppSettingNames.AppName, tenant.TenancyName, tenant.Id);
            await AddSettingIfNotExists(AppSettingNames.AppLogo, GetLogoBase64(), tenant.Id);
            await AddSettingIfNotExists(AppSettingNames.AppLogoSmall, GetLogoSmallBase64(), tenant.Id);
            await AddSettingIfNotExists(AppSettingNames.AppLogoName, tenant.TenancyName, tenant.Id);
            await AddSettingIfNotExists(AppSettingNames.AppSocialMediaAuth, "false", tenant.Id);
            await AddSettingIfNotExists(AppSettingNames.AppEnableTwoFactor, "false", tenant.Id);
            await AddSettingIfNotExists(AppSettingNames.AppChangeLogoImage, "true", tenant.Id);

            return tenantDto;
        }

        protected override IQueryable<Tenant> CreateFilteredQuery(PagedTenantResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.TenancyName.Contains(input.Keyword) || x.Name.Contains(input.Keyword))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
        }

        protected override void MapToEntity(TenantDto updateInput, Tenant entity)
        {
            // Manually mapped since TenantDto contains non-editable properties too.
            entity.Name = updateInput.Name;
            entity.TenancyName = updateInput.TenancyName;
            entity.IsActive = updateInput.IsActive;
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var tenant = await _tenantManager.GetByIdAsync(input.Id);
            await _tenantManager.DeleteAsync(tenant);
        }

        private void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        [AbpAuthorize(PermissionNames.Pages_Tenants)]
        public async Task<bool> ChangeTenantActivation(int id)
        {
            var tenant = await _tenantManager.GetByIdAsync(id);
            tenant.IsActive = !tenant.IsActive;

            CurrentUnitOfWork.SaveChanges();
            return tenant.IsActive;
        }

        private async Task AddSettingIfNotExists(string name, string value, int? tenantId = null)
        {
            SettingInfo setting = new SettingInfo(tenantId, null, name, value);
            await _settingStore.CreateAsync(setting);
        }

        private string GetLogoBase64()
        {
            return "data:image/png;base64,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";
        }
        private string GetLogoSmallBase64()
        {
            return "data:image/png;base64,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";
        }
    }
}


