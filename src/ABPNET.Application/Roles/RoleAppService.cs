using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.UI;
using ABPNET.Authorization;
using ABPNET.Authorization.Roles;
using ABPNET.Authorization.Users;
using ABPNET.Roles.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ABPNET.Roles
{
    [AbpAuthorize]
    public class RoleAppService : AsyncCrudAppService<Role, RoleDto, int, PagedRoleResultRequestDto, CreateRoleDto, RoleDto>, IRoleAppService
    {
        private readonly RoleManager _roleManager;
        private readonly UserManager _userManager;

        public RoleAppService(IRepository<Role> repository, RoleManager roleManager, UserManager userManager)
            : base(repository)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public override async Task<RoleDto> CreateAsync(CreateRoleDto input)
        {
            CheckCreatePermission();

            var role = ObjectMapper.Map<Role>(input);
            role.SetNormalizedName();

            CheckErrors(await _roleManager.CreateAsync(role));

            var grantedPermissions = PermissionManager
                .GetAllPermissions()
                .Where(p => input.GrantedPermissions.Contains(p.Name))
                .ToList();

            await _roleManager.SetGrantedPermissionsAsync(role, grantedPermissions);

            return MapToEntityDto(role);
        }

        public async Task<ListResultDto<RoleListDto>> GetRolesAsync(GetRolesInput input)
        {
            var roles = await _roleManager
                .Roles
                .WhereIf(!_userManager.IsSuperAdminCurrentUser(), x => x.Id != _roleManager.GetSuperAdminRole().Id)
                .WhereIf(
                    !input.Permission.IsNullOrWhiteSpace(),
                    r => r.Permissions.Any(rp => rp.Name == input.Permission && rp.IsGranted)
                )
                .ToListAsync();

            return new ListResultDto<RoleListDto>(ObjectMapper.Map<List<RoleListDto>>(roles));
        }

        public override async Task<RoleDto> UpdateAsync(RoleDto input)
        {
            CheckUserViewOrChangeSuperAdminRole(input.Id);
            CheckUpdatePermission();

            var role = await _roleManager.GetRoleByIdAsync(input.Id);

            ObjectMapper.Map(input, role);

            CheckErrors(await _roleManager.UpdateAsync(role));

            var grantedPermissions = PermissionManager
                .GetAllPermissions()
                .Where(p => input.GrantedPermissions.Contains(p.Name))
                .ToList();

            await _roleManager.SetGrantedPermissionsAsync(role, grantedPermissions);

            return MapToEntityDto(role);
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var role = await _roleManager.FindByIdAsync(input.Id.ToString());
            var users = await _userManager.GetUsersInRoleAsync(role.NormalizedName);

            foreach (var user in users)
            {
                CheckErrors(await _userManager.RemoveFromRoleAsync(user, role.NormalizedName));
            }

            CheckErrors(await _roleManager.DeleteAsync(role));
        }

        public Task<ListResultDto<PermissionDto>> GetAllPermissions()
        {
            var permissions = _userManager.IsSuperAdminCurrentUser()
                            ? PermissionManager.GetAllPermissions().Where(x =>
                            !PermissionNames.IsPermissionApiClientUser(x.Name)
                            )
                            : PermissionManager.GetAllPermissions().Where(x =>
                            !PermissionNames.IsPermissionChangeApplicationSettings(x.Name) &&
                            !PermissionNames.IsPermissionChangeShowTentant(x.Name) &&
                            !PermissionNames.IsPermissionChangeLogoImage(x.Name) &&
                            !PermissionNames.IsPermissionApiClientUser(x.Name)
                            );

            return Task.FromResult(new ListResultDto<PermissionDto>(
                ObjectMapper.Map<List<PermissionDto>>(permissions).OrderBy(p => p.DisplayName).ToList()
            ));
        }

        protected override IQueryable<Role> CreateFilteredQuery(PagedRoleResultRequestDto input)
        {
            var keyword = input?.Keyword?.ToLower();

            return Repository.GetAllIncluding(x => x.Permissions)
                .Where(x => x.Name != ABPNETConsts.DefaultApiUserName) // hide system ApiUser
                .WhereIf(!_userManager.IsSuperAdminCurrentUser(), x => x.Id != _roleManager.GetSuperAdminRole().Id)
                .WhereIf(!keyword.IsNullOrWhiteSpace(), x => x.Name.ToLower().Contains(keyword)
                || x.DisplayName.ToLower().Contains(keyword)
                || x.Description.ToLower().Contains(keyword));
        }

        protected override async Task<Role> GetEntityByIdAsync(int id)
        {
            CheckUserViewOrChangeSuperAdminRole(id);
            return await Repository.GetAllIncluding(x => x.Permissions).FirstOrDefaultAsync(x => x.Id == id);
        }

        protected override IQueryable<Role> ApplySorting(IQueryable<Role> query, PagedRoleResultRequestDto input)
        {
            switch (input.Sorting)
            {
                case "Name":
                    return input.Descending ? query.OrderByDescending(x => x.Name.ToLower()) : query.OrderBy(x => x.Name.ToLower());
                case "DisplayName":
                    return input.Descending ? query.OrderByDescending(x => x.DisplayName.ToLower()) : query.OrderBy(x => x.DisplayName.ToLower());
                case "Description":
                    return input.Descending ? query.OrderByDescending(x => x.Description.ToLower()) : query.OrderBy(x => x.Description.ToLower());
                default:
                    return query.OrderBy(x => x.DisplayName);
            }
        }

        protected virtual void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        public async Task<GetRoleForEditOutput> GetRoleForEdit(EntityDto input)
        {
            var permissions = _userManager.IsSuperAdminCurrentUser()
                ? PermissionManager.GetAllPermissions().Where(x =>
                            !PermissionNames.IsPermissionApiClientUser(x.Name)
                            )
                : PermissionManager.GetAllPermissions().Where(x =>
                            !PermissionNames.IsPermissionChangeApplicationSettings(x.Name) &&
                            !PermissionNames.IsPermissionChangeShowTentant(x.Name) &&
                            !PermissionNames.IsPermissionChangeLogoImage(x.Name) &&
                            !PermissionNames.IsPermissionApiClientUser(x.Name)
                            );

            var role = await _roleManager.GetRoleByIdAsync(input.Id);
            var grantedPermissions = (await _roleManager.GetGrantedPermissionsAsync(role)).ToList();
            var roleEditDto = ObjectMapper.Map<RoleEditDto>(role);

            //Get permissions list
            var permissionList = ObjectMapper.Map<List<FlatPermissionDto>>(permissions).OrderBy(p => p.DisplayName).ToList();
            List<FlatPermissionDto> removePermissionList = new List<FlatPermissionDto>();

            //Remove components without an active licence
            foreach (var item in removePermissionList)
            {
                permissionList.Remove(item);
                grantedPermissions = grantedPermissions.Where(x => x.Name != item.Name).ToList();
            }

            return new GetRoleForEditOutput
            {
                Role = roleEditDto,
                Permissions = permissionList,
                GrantedPermissionNames = grantedPermissions.Select(p => p.Name).ToList()
            };
        }

        private void CheckUserViewOrChangeSuperAdminRole(int roleId)
        {
            if (!_userManager.IsSuperAdminCurrentUser() && roleId == _roleManager.GetSuperAdminRole().Id)
                throw new UserFriendlyException(this.L("UsersCantChangeSuperAdminRoles"));
        }

        public async Task<ListResultDto<RoleDto>> GetAllForNotification()
        {
            var roles = await Repository.GetAllListAsync();
            return new ListResultDto<RoleDto>(ObjectMapper.Map<List<RoleDto>>(roles));
        }
    }
}




