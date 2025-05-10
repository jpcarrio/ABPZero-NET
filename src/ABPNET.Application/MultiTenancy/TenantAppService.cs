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
using ABPNET.Authorization;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;
using ABPNET.Configuration;
using ABPNET.Editions;
using ABPNET.MultiTenancy.Dto;
using Microsoft.AspNetCore.Identity;

namespace ABPNET.MultiTenancy
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
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAUAAAACGCAMAAACbiKOFAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA2ZpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuMy1jMDExIDY2LjE0NTY2MSwgMjAxMi8wMi8wNi0xNDo1NjoyNyAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo1MTA5QTQ5MDEwNTBFNzExQTdEOUMwMDNGNkZGMDk0MSIgeG1wTU06RG9jdW1lbnRJRD0ieG1wLmRpZDo3RUQ1NkRENjUwQkMxMUU3ODQ3Mzg1NTFFMzdGREFBMiIgeG1wTU06SW5zdGFuY2VJRD0ieG1wLmlpZDo3RUQ1NkRENTUwQkMxMUU3ODQ3Mzg1NTFFMzdGREFBMiIgeG1wOkNyZWF0b3JUb29sPSJBZG9iZSBQaG90b3Nob3AgQ1M2IChXaW5kb3dzKSI+IDx4bXBNTTpEZXJpdmVkRnJvbSBzdFJlZjppbnN0YW5jZUlEPSJ4bXAuaWlkOkVFODhERDU5QkE1MEU3MTE4MUIzQ0YwOUFGOUU3REU2IiBzdFJlZjpkb2N1bWVudElEPSJ4bXAuZGlkOjUxMDlBNDkwMTA1MEU3MTFBN0Q5QzAwM0Y2RkYwOTQxIi8+IDwvcmRmOkRlc2NyaXB0aW9uPiA8L3JkZjpSREY+IDwveDp4bXBtZXRhPiA8P3hwYWNrZXQgZW5kPSJyIj8++WnInAAAAbBQTFRFhYWFh4eH/86w/6Rt7+/viIiIhISEg4OD//Hopqam39/fz8/PioqKjo6O/+PSlpaWx8fH/9W7/8Ca//j0iYmJ9/f3gYGBgICAgoKC/6t4/51hi4uLnp6e/9zH/8el/+rdf39//7KDjIyM5+fnvr6+rq6u19fXfn5+7OzsjY2N/7mOfX19tra2e3t7kpKSqKio6urq5eXlj4+Pt7e3k5OT8vLynZ2dfHx84+PjkJCQmZmZn5+f4uLi4eHhlJSUzs7Oqampenp6xsbG9fX1+fn5nJycoaGh7u7u8fHxkZGRtLS06enp09PT3d3d8/Pz29vb0NDQ4ODg0tLSeXl5+vr6m5ubpKSksbGx5ubmxMTE1NTU/Pz8ycnJ1dXVmJiYxcXFmpqauLi4urq6v7+/srKyl5eXpaWlvb29oKCg8PDw9PT0q6ur////d3d3wcHBlZWVvLy8y8vL/v7+3Nzc7e3t9vb2rKys+Pj42trar6+vubm5dnZ22dnZo6Oj0dHRwMDA6+vr/f39eHh4wsLC3t7eoqKiysrK+/v7tbW1c3NzzMzMu7u76Ojo/5ZWhoaG////e46yNQAAAJB0Uk5T//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////8AA2XnhwAAECtJREFUeNrsnfdD4kq7x2cCMxFNECQk0YggiIiu7qpr1+29nO31bDu9997fXi+Tf/kGUphJJiFw9twj52Z2f6Alznx4yvd5ZnYFZjJ+1QAJggTg0AN8jbyWAPw1gxCSAEwA/rYAL25ff1r7JgE4CMBzt/6T+/TBTzvy5mgCcACAl56cR6ACqkiXH72fAOwf4OoeUBVM5GpFPf9FAnAAgPcwPG5ggiURvlNMAPbvwt8ZGlKXMVpRKheuJgD7TyJPxkVZltbWdoyKfPKrBGDfAPf3KopsIF2SsHZ/IQHYN8APH+GKCKsSktBfmvsJwP6F9P67mtaU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";
        }
        private string GetLogoSmallBase64()
        {
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAUAAAACGCAMAAACbiKOFAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA2ZpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuMy1jMDExIDY2LjE0NTY2MSwgMjAxMi8wMi8wNi0xNDo1NjoyNyAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo1MTA5QTQ5MDEwNTBFNzExQTdEOUMwMDNGNkZGMDk0MSIgeG1wTU06RG9jdW1lbnRJRD0ieG1wLmRpZDo3RUQ1NkRENjUwQkMxMUU3ODQ3Mzg1NTFFMzdGREFBMiIgeG1wTU06SW5zdGFuY2VJRD0ieG1wLmlpZDo3RUQ1NkRENTUwQkMxMUU3ODQ3Mzg1NTFFMzdGREFBMiIgeG1wOkNyZWF0b3JUb29sPSJBZG9iZSBQaG90b3Nob3AgQ1M2IChXaW5kb3dzKSI+IDx4bXBNTTpEZXJpdmVkRnJvbSBzdFJlZjppbnN0YW5jZUlEPSJ4bXAuaWlkOkVFODhERDU5QkE1MEU3MTE4MUIzQ0YwOUFGOUU3REU2IiBzdFJlZjpkb2N1bWVudElEPSJ4bXAuZGlkOjUxMDlBNDkwMTA1MEU3MTFBN0Q5QzAwM0Y2RkYwOTQxIi8+IDwvcmRmOkRlc2NyaXB0aW9uPiA8L3JkZjpSREY+IDwveDp4bXBtZXRhPiA8P3hwYWNrZXQgZW5kPSJyIj8++WnInAAAAbBQTFRFhYWFh4eH/86w/6Rt7+/viIiIhISEg4OD//Hopqam39/fz8/PioqKjo6O/+PSlpaWx8fH/9W7/8Ca//j0iYmJ9/f3gYGBgICAgoKC/6t4/51hi4uLnp6e/9zH/8el/+rdf39//7KDjIyM5+fnvr6+rq6u19fXfn5+7OzsjY2N/7mOfX19tra2e3t7kpKSqKio6urq5eXlj4+Pt7e3k5OT8vLynZ2dfHx84+PjkJCQmZmZn5+f4uLi4eHhlJSUzs7Oqampenp6xsbG9fX1+fn5nJycoaGh7u7u8fHxkZGRtLS06enp09PT3d3d8/Pz29vb0NDQ4ODg0tLSeXl5+vr6m5ubpKSksbGx5ubmxMTE1NTU/Pz8ycnJ1dXVmJiYxcXFmpqauLi4urq6v7+/srKyl5eXpaWlvb29oKCg8PDw9PT0q6ur////d3d3wcHBlZWVvLy8y8vL/v7+3Nzc7e3t9vb2rKys+Pj42trar6+vubm5dnZ22dnZo6Oj0dHRwMDA6+vr/f39eHh4wsLC3t7eoqKiysrK+/v7tbW1c3NzzMzMu7u76Ojo/5ZWhoaG////e46yNQAAAJB0Uk5T//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////8AA2XnhwAAECtJREFUeNrsnfdD4kq7x2cCMxFNECQk0YggiIiu7qpr1+29nO31bDu9997fXi+Tf/kGUphJJiFw9twj52Z2f6Alznx4yvd5ZnYFZjJ+1QAJggTg0AN8jbyWAPw1gxCSAEwA/rYAL25ff1r7JgE4CMBzt/6T+/TBTzvy5mgCcACAl56cR6ACqkiXH72fAOwf4OoeUBVM5GpFPf9FAnAAgPcwPG5ggiURvlNMAPbvwt8ZGlKXMVpRKheuJgD7TyJPxkVZltbWdoyKfPKrBGDfAPf3KopsIF2SsHZ/IQHYN8APH+GKCKsSktBfmvsJwP6F9P67mtaUoQQUUfwlAdg/wI2PjUZzByvYIPrXCwnA/ku5W5sVUlWRWK1q49cTgP0DPPvksg4UCRhyBZ5MAA7QTLi4ThpVAyiyrj5PAA4A0LxxT8UKhFUgvp4AHATgK9cuEKwAyQCnFhKAAwA07157VkFE2UHTXyQABwFovr96Cm01m0vS158nAAcBaJoHDwwANHRvNAE4GEDziz1dIwo8dS4BOBjAi+8YarVaefanBOBgAM2Dd0XUFC//4xvTvHTmv0edkUkAxh3nniu6YqiblpJZAH/ZWlpa2traWtpKAMZAt39m/W93ze09QJTTxs+m+dUHuysrx62xcnwlARijErmPtJ8emhvv7GB1S/pu1fLh9J+O2eNqArD3+A4gXTr5ubm6qyH0g/xjkkT6BJhTNAgvnDy4dl/Sq41qbiMB2B/A7U0NAbI2ndlrVqoiPv/XG9urC874MgHYexQ/uywShJAkIYyxLhpvja9hLLb/iJgPsE6cMce9Yy0357w/nculnRfThB1HcoGyZ9R9b5552b0yzb9Re4xSFwdH7jeWMe+tQx1KSNUwVBVF1MTGv2H750IICRdg0ZuazAsJMjv9TOi65VwIQLk4VADNgwt6o4IlBSFYlSWIQAW1h/UccQGOdec2H7Ai2T/9cICWhaa5AMmR4QL4zdM1dRk3DUiqyDI7gAzJHVyAGcJfKcs2DkAip7kAyehQATQ3LqxBRRIrwFCXRKIT2cLYRgkhDyC9AtbZzBrpEyCZ5gMcLw4VwA9vPFNliUjEwWbhcx7wAOboybE+PN43QDLGBUinpyEAaF78XsJWGnYAIpsgCkki0zYXe3J1+p35bnqoddbaTsc+gGP2GzLHBBkGtV4Ax8IW48xs/P+0mbC/uYWbCrDxOdYXAtBxU9cOaR/21Av14tgcd93FaRK4wSjft4cCYPHTimYQ13M9R+YBdCjVpoPryHBSgB/DmD+SjvIBdh1vKACa1z8RCZYg9IwPtpUMD+C4owBzQR8eD7hfOECP9lgIQJIeKoDvrUsqkJz0gZATBjlJZNTVL7WgD09HFCiBdR8JAzieYbLPkAA0n66oHXZUBOS68BEv+04Hclw9Iu/FByin2bsMCcDtXd0gkB78JCJ7+m+ONRRG4GRqPQFOB4SQ68LufRyVPSQAL70jaS441K6EO38DAOe7ka/mi1WsRKuPxkwiwSzsBdP6MAE0b59QkSegbYIcgHVq9nLAX+foJDCeiwDoyRiODux+N/PxhPTYoQC4fb7hREAEHfsLAizSZjMXrMam2Up3LAQgJaTHeADdW3cqxWEBuPDuR5iKgM5afADH6Lg3H1QtxYyv1K2ZZt+1cPs+491mxbAA3Dh6p5M42jZIXIR+gBnGa2VOzs35Fjdv9t2NoR+PDg9A83utaYvAThLpdFT9AH1rqfOmmj4SKPRDAco1MwSgq3LGi8MD8JEoe3WcEwT9QjrH+t08v/IoMk1pp+OV7tlPZQG6Tjw3PACfaIpXBdsWGADo087F0MpjbNpf1AbXPT4Wsifi+3aGRcaY6wQQ6CpAyNOBtZCGG29rZN7XsEr795SCBTML0A0Q08VhAXgFi87coLdOFuBcWMuS235hG1Y91x0AmJbZxtlhB1isq7izE0c8EeNvJoyHATxiRrQP7BDZP0BXM8nycABMv90BSDwRHbDA8K65zL3jNG2fAwA0M7z21qEFOLqpOuQoggzAI+HbDrbam6tx+g7OygcBmJaHCODd5ydUT73wLVAOB1h3pn6kFpTUcsS6i+2wOlcMAciq8kMO8P23FQ045scHOB/OzxF7mU4LwV5ZLsPC5a7bEXvOJmYQIFNbH3KAF8fFCvbkCy+J1Hkpt04vJxORornrnmOEJAdgOhwgMw5BS//BmqjLXRVjo6QtsMhNGPO0mWXCvZsPkO1pcQDSTny4AT7cJIhASgNCvwuPcSVLkRZ7PIDuFueAACnpdLgB3kRLVanCiGhfJZLhnyeqUwVbJtT+BnVhuvo51AAXTlWWJEV3EnDXCrsA0yGSb4w6wTFf9x/rGDUjAfZMInT5c5gBfn5SVgEG3l4ItGMgnURyhHOWg/Zhe31j3uHKTC4XuakUR8bQbZlDDfDLjAYlDTSd9OFlEpgc8Y01Xs1drhhEVBT6S4WsCycAowD+HWJJJTLu5l8nFiYAY41vEQLWcPeDXX4EggRgrEbWGxa5djMVuQGQuLEwARhnHHsBfPrFLUQSC4w1fpQsBdM+zIbZHJIAjPexjddF0AEIQNcIbV9OAMYZt8YrwG5hAehrqSYAY5XBIrbp2WHQ2xDhHi4abORTrdZs1nliPW4P4WUv9+XfNx7Au/9YlhTokzGOD78kC5yxl1b6YwJcyCh6t5FlP+hEQwspZYEjLXZMCrFn6l6a+mMCrO0pIoSeArQ92GIHogG2x0whngO7nx/5QwK8fgK4DXzvXEKHXhtiNMDWVHZoAWaFyezLAfgzwojQO3Ed/dIxw54AW63yMLpwQZhof/IlATxTwd0z0W4gdAywN8BUnB8hHK4k4i7lJQF84zSwu6fdbmoniXSiYE+ALpVhkjEvGeDrdxBgCuB2F6Ztfm2GQYD5zhNhykskhyDY/84AT7cBOvCgm0M6/HgWmHeeOtNtzf6/B3hGazQdEQNd9dL23xAdmGfjWgLQPNPYAk7qgMBTgLYBhlugmXWeLzJTs9Nba4pV2ezSOAvN21/HolDgXTQiOF8UdWW5E0RSQjkKYFawa6CU0I3U/hg+ETGJmP/xTkOl21hu/u3UIhEWWGoF5luaZGZWiAswP8W7yvtY3rP07pUT3gWLhRCA2QmmcMr2AMibRDyAn+HueRjPeV0pGA7QqW+nupgW/Sq7FAtgYZbVRWXfRaVWECB9yWSB+1OyflL5KID8ScQD+LiKvH8XApgIGOXCWb8BFiaDEicfA2DguqkR9mOpIED2q5qMB9AJeVyAIZMIB5g+tn3rxrXb869Yj/ffEjWvA227r8MvQsbMBBx4kicSs70BBq+bZT5GZ3vfS61WxH2zIZqfCzBkEkGAGwsPD27cPvP9B+/uHleW7px+ar126ZTaoEs54LVielcieX+x4Z9zoRdA97pF6ysvpei7poIrSkVWQ0GAk50cMzLh0/yBLBw2CRrgq+fSBzf/+vXecUlRiAgxRkjUcb1tgm9090LcAAjcEQlwZqRbXbohOCXQBuqsJhyge50tx0emKBNMMRZWztMvTQlMzipxAXqljxuvZ0IAhk7CA7ixevvK+cuygTyJ4pS+6IxF8GZVs/IwsESM6iVgF2IkQCFogG5EzzLGEQ5QYCtqZ6UFBmDZn81bqRHW/Wd4APNU84ANln6AoZNoA/xq9eQv/zq1eRypKgYiJVE6Q9u99mfz4B5p4LYGBNgOgF4E7OnCU3k2Ak55NjlBrz4c4CQbSkuURaWCOinlW/kIk0YihLTzVgjA0ElYAM8+/skQlz6CekPDWFddz/QAnt7c/vO5J+3fCwSJCKBrfxFJhK8CAqq0TC8/FGCBaRR6hktf1Apq61l/5poKB5gVBMFz9QIXYPgkLIAPM40fdCg2DcmoGkjs/jtW+0A+WhIv7H+4UNUxRETEkidfSM+WPtNMKAdbM3RcCQVYjvhWUqzfUS91+xeunfMBCn5lOsIFGD4JC+Cxf+Ef1Oqaruqg/Z8sEuYAICFVoqlvL2y80SRWUtFpgCBaBxa67ZgsFfKChdVEJMBSb4ATQYB5/26VTcYHMB+8Lx9gKQqgufom+WhZxBhgERDv/zPxTlACoIP7T2v3DJ0oQFTcCi5GLexpz0UK4EifAPO9AQpxABaCFyy24gLMRwI0r358XtKNZrOKSIcg8v5BsGWBDSThpcrm7VOXG9iAGBDaANmNdT9Az/KnIi1w5jcFGO7Cni61YmDZfWsggObZW+snTjcwsqzN4+fK5uWV6nKlou6sH1/Sq4ooevxI6KZS3h/l2l8/JwbGSiKlgQAKvZNIocXbRRjEhTsIr67f3xFVYlRFqFiGiIy2ntExkUBXMhPqcfdhTICFQHs62woqkvAkUo7X3kv5OmgFpvjj6EuBnWp0EilH9ANfTZ/MrEBREzFQJKieFgmCWFVVymMd7dwthzt/IwB6xSZtCgW/cxWideCU/1uJA9D7KUKokJ5hmBT4MTDP4s1HNlTvbmw/P4+gtlxR1poKhsbaWtNQiIeOAge6qTgCoNfTn2SqSV+3cLZHJbIY1Cq9AbpCMMv2LOgLJhiAAh/gItuHm+zVkX5l9cc396paAytGo2I5MLEqE8pxid+To5LIiMD2QjyVONl+vzwRvjT2WT6gwM280BNgp8wtzEQ0Eyboi8u+LFygpl4SIiYR7MakV2/WT+jLd6S2O4ui7rmvCwx0W9IExNrWdGY1w31vtmc7yytv7UZ6ydKXE70BcntCHBfuOLfglzFM92oiYhLcfuCl6x9nNi0B3dChAj3ndeQf8XQMidGNYfqivMX5e6OcZ9mw6jA+wFmOz2cjvmv2256ImERIQ/Vc+tipC2/tyJK+RLsu8FqB7p84Rzs8sx8Jrm6qHKMjLfQPkG1/uq0ZvmXbF7AyhlnLRMQkwjvSr579n/lHR1cw5bGga42uCXL3RDiVsPOBWf8BuHKsPZFS3wAFgbeHwl5QZhD7ANLAJiIm0WNP5OL29TePntDuNAgiIlEQxpKoYwh02MRWgGz/MgIxGuAsK53ojS1eGyokO4ywVddsqSdACsBi2N5ftptxRkw/wO4NXC/hTiLGplL64OajKy8g1iq6tqwBIIoI6lZ+wTZCHAFQEAqcM2OzzunLUj/7wgVn/5Y5tRlZidhnSyaFkQjdY89lphSoRGwZkepmjbBJxDwj/c+Fx1eOTu/uICJpy0sYAU216WHaAg/D+K3ONPyqkwkX3zt79hXz/YeP118o4icSkJqSaJsg68IJwJDx6bPje/U3H6w/P3lt/tsrf/9kZU3p+HBigTEBHjy4LGpNiFZ2d3dfnF+RrPoOE8eBMU4A9h53j/28LoOtilppVE7fWVaxjhX7FwIlAON+cOHxL5kXK0i0coiIwNKWav9CpQRg/I/+8+zV+TNXPth99taJHblqtM+7iWpjuZIA7GucSy/U9r+9/Vmu/sHbF+6dkA2YABxwbKS/vLq6f+tvCcBfOxKACcAE4O82EoAJwARgAjABmABMAA4zwNfIawnAZCQAf5/xvwIMAMHH+RFTxNI7AAAAAElFTkSuQmCC";
        }
    }
}




