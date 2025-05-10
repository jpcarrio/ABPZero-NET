using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.UI;
using ISUCore.Authorization;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;
using ISUCore.Roles.Dto;
using ISUCore.Users.Dto;
using Medical.Users.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ISUCore.Users
{
    [AbpAuthorize]
    public class UserAppService : AsyncCrudAppService<User, UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>, IUserAppService
    {
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IRepository<Role> _roleRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAbpSession _abpSession;
        private readonly LogInManager _logInManager;

        public UserAppService(
            IRepository<User, long> repository,
            UserManager userManager,
            RoleManager roleManager,
            IRepository<Role> roleRepository,
            IPasswordHasher<User> passwordHasher,
            IAbpSession abpSession,
            LogInManager logInManager)
            : base(repository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _abpSession = abpSession;
            _logInManager = logInManager;
        }

        public override async Task<UserDto> CreateAsync(CreateUserDto input)
        {
            if (string.IsNullOrEmpty(input.Password))
            {
                input.Password = ISUCore.Authorization.Users.User.DefaultPassword;
            }

            CheckCreatePermission();

            var user = ObjectMapper.Map<User>(input);

            user.TenantId = AbpSession.TenantId;
            user.IsEmailConfirmed = false;

            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            CheckErrors(await _userManager.CreateAsync(user, input.Password));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            CurrentUnitOfWork.SaveChanges();

            return MapToEntityDto(user);
        }

        public override async Task<UserDto> UpdateAsync(UserDto input)
        {
            CheckUserViewOrChangeSuperAdminUsers(input.Id);

            CheckUpdatePermission();

            var user = await _userManager.GetUserByIdAsync(input.Id);

            var isEmailConfirmed = CheckIsEmailConfirmed(input, user);

            MapToEntity(input, user);

            user.IsEmailConfirmed = isEmailConfirmed;

            CheckErrors(await _userManager.UpdateAsync(user));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            return await GetAsync(input);
        }

        public override async Task DeleteAsync(EntityDto<long> input)
        {
            CheckUserViewOrChangeSuperAdminUsers(input.Id);

            CheckDeletePermission();

            var user = await _userManager.GetUserByIdAsync(input.Id);
            await Repository.DeleteAsync(user);
        }

        [AbpAuthorize(PermissionNames.Pages_Users_Activation)]
        public async Task Activate(EntityDto<long> user)
        {
            await Repository.UpdateAsync(user.Id, (entity) =>
            {
                entity.IsActive = true;
                return Task.CompletedTask;
            });
        }

        [AbpAuthorize(PermissionNames.Pages_Users_Activation)]
        public async Task DeActivate(EntityDto<long> user)
        {
            await Repository.UpdateAsync(user.Id, (entity) =>
            {
                entity.IsActive = false;
                return Task.CompletedTask;
            });
        }

        public async Task<ListResultDto<RoleDto>> GetRoles()
        {
            var roles = await _roleRepository.GetAllListAsync();
            // only super Admin users can view Super Admin Roles
            var rolesItems = roles.WhereIf(!_userManager.IsSuperAdminCurrentUser(), x => x.Id != _roleManager.GetSuperAdminRole().Id);
            return new ListResultDto<RoleDto>(ObjectMapper.Map<List<RoleDto>>(rolesItems));
        }

        public async Task<List<RoleDto>> GetAllNotAdminRoles()
        {
            return await Task.Run(() =>
            {
                var result = new List<RoleDto>();
                var roles = _roleRepository.GetAllList()
                                .Where(x => x.Name != ISUCoreConsts.DefaultApiUserName);
                if (roles.Any())
                {
                    var rolesItems = roles.Where(x =>
                        x.Id != _roleManager.GetSuperAdminRole().Id && x.Id != _roleManager.GetAdminRole().Id);
                    result = ObjectMapper.Map<List<RoleDto>>(rolesItems);
                }
                return result;
            });
        }

        public async Task<List<RoleDto>> GetNotAdminRolesByUserId(long id)
        {
            return await Task.Run(() =>
            {
                var result = new List<RoleDto>();
                var user = Repository.GetAllIncluding(x => x.Roles)
                    .Where(x => x.Name != ISUCoreConsts.DefaultApiUserName)
                    .FirstOrDefault(x => x.Id == id);
                if (user != null)
                {
                    var roles = _roleRepository.GetAllList();
                    if (roles.Any())
                    {
                        var rolesItems = roles.Where(x =>
                            x.Id != _roleManager.GetSuperAdminRole().Id && x.Id != _roleManager.GetAdminRole().Id);
                        var roleIds = user.Roles.Select(x => x.RoleId).ToList();
                        var userRoles = rolesItems.Where(x => roleIds.Contains(x.Id));
                        result = ObjectMapper.Map<List<RoleDto>>(userRoles);
                    }
                }
                return result;
            });
        }

        [AbpAllowAnonymous]
        public async Task ChangeLanguage(ChangeUserLanguageDto input)
        {
            await SettingManager.ChangeSettingForUserAsync(
                AbpSession.ToUserIdentifier(),
                LocalizationSettingNames.DefaultLanguage,
                input.LanguageName
            );
        }

        protected override User MapToEntity(CreateUserDto createInput)
        {
            var user = ObjectMapper.Map<User>(createInput);
            user.SetNormalizedNames();
            return user;
        }

        protected override void MapToEntity(UserDto input, User user)
        {
            ObjectMapper.Map(input, user);
            user.SetNormalizedNames();
        }

        protected override UserDto MapToEntityDto(User user)
        {
            var roleIds = user.Roles.Select(x => x.RoleId).ToArray();

            var roles = _roleManager.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.NormalizedName);

            var userDto = base.MapToEntityDto(user);
            userDto.RoleNames = roles.ToArray();

            return userDto;
        }

        protected override IQueryable<User> CreateFilteredQuery(PagedUserResultRequestDto input)
        {
            string keyword = input?.Keyword?.ToLower();
            var inputRoleId = _roleManager.Roles.FirstOrDefault(x => keyword.IsNullOrEmpty() || x.DisplayName.ToLower().Contains(keyword))?.Id;

            var result = Repository.GetAllIncluding(x => x.Roles)
                .Where(x => x.Name != ISUCoreConsts.DefaultApiUserName) // hide system ApiUser
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.UserName.ToLower().Contains(keyword)
                            || x.Name.ToLower().Contains(keyword)
                            || x.Surname.ToLower().Contains(keyword)
                            || (x.Name.ToLower() + " " + x.Surname.ToLower()).Contains(keyword)
                            || x.EmailAddress.ToLower().Contains(keyword)
                            || x.Roles.Any(r => r.RoleId == inputRoleId))
                .WhereIf(!_userManager.IsSuperAdminCurrentUser(), x => !x.Roles.Any(s => s.RoleId == _roleManager.GetSuperAdminRole().Id))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
            return result;
        }

        protected override async Task<User> GetEntityByIdAsync(long id)
        {
            var user = await Repository.GetAllIncluding(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                throw new EntityNotFoundException(typeof(User), id);
            }

            return user;
        }

        public async Task<UserDto> GetById(int? tenantId, long userId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                User user = await _userManager.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId);
                if (user == null)
                {
                    throw new EntityNotFoundException(typeof(User), userId);

                }
                return ObjectMapper.Map<UserDto>(user);
            }
        }

        protected override IQueryable<User> ApplySorting(IQueryable<User> query, PagedUserResultRequestDto input)
        {
            return input.Sorting switch
            {
                "UserName" => input.Descending ? query.OrderByDescending(x => x.Name.ToLower()) : query.OrderBy(x => x.Name.ToLower()),
                "Surname" => input.Descending ? query.OrderByDescending(x => x.Surname.ToLower()) : query.OrderBy(x => x.Surname.ToLower()),
                "Name" => input.Descending ? query.OrderByDescending(x => x.Name.ToLower()) : query.OrderBy(x => x.Name.ToLower()),
                "EmailAddress" => input.Descending ? query.OrderByDescending(x => x.EmailAddress.ToLower()) : query.OrderBy(x => x.EmailAddress.ToLower()),
                "IsEmailConfirmed" => input.Descending ? query.OrderByDescending(x => x.IsEmailConfirmed) : query.OrderBy(x => x.IsEmailConfirmed),
                "IsActive" => input.Descending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                _ => query.OrderBy(x => x.Id),
            };
        }

        protected virtual void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }

        public async Task<bool> ResetPassword(ResetPasswordDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attempting to reset password.");
            }

            var currentUser = await _userManager.GetUserByIdAsync(_abpSession.GetUserId());
            var loginAsync = await _logInManager.LoginAsync(currentUser.UserName, input.AdminPassword, shouldLockout: false);
            if (loginAsync.Result != AbpLoginResultType.Success)
            {
                throw new UserFriendlyException("Your 'Admin Password' did not match the one on record.  Please try again.");
            }

            if (currentUser.IsDeleted || !currentUser.IsActive)
            {
                return false;
            }

            var roles = await _userManager.GetRolesAsync(currentUser);
            if (!roles.Contains(StaticRoleNames.Tenants.Admin))
            {
                throw new UserFriendlyException("Only administrators may reset passwords.");
            }

            var user = await _userManager.GetUserByIdAsync(input.UserId);
            if (user != null)
            {
                user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return true;
        }

        [AbpAllowAnonymous]
        public async Task<bool> ChangeUserImage(ChangeUserImageDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attemping to change user image.");
            }

            long userId = _abpSession.UserId.Value;
            var user = await _userManager.GetUserByIdAsync(userId);

            user.UserImage = input.UserImage;
            user.UserImageFileName = input.UserImageFileName;

            CurrentUnitOfWork.SaveChanges();
            return true;
        }

        public bool IsConnectionAlive() => true;


        [AbpAuthorize(PermissionNames.Pages_Users_Activation)]
        public async Task<bool> ChangeUserAccountActivation(long id)
        {
            var user = await _userManager.GetUserByIdAsync(id);
            user.IsActive = !user.IsActive;

            CurrentUnitOfWork.SaveChanges();
            return user.IsActive;
        }

        private void CheckUserViewOrChangeSuperAdminUsers(long userId)
        {
            var isControllingSuperAdminUser = this._userManager.IsInRoleAsync(this._userManager.GetUserById(userId), StaticRoleNames.Tenants.SuperAdmin).Result;
            if (!_userManager.IsSuperAdminCurrentUser() && isControllingSuperAdminUser)
                throw new UserFriendlyException(this.L("UsersCantChangeSuperAdminRoles"));
        }

        public async Task BatchCreateAsync(List<ImportUserDto> input)
        {
            CheckCreatePermission();

            foreach (var item in input)
            {
                if (item.Id == 0)
                {
                    var user = ObjectMapper.Map<User>(item);
                    // TODO: Set default password
                    var password = "123qwe!";

                    user.TenantId = AbpSession.TenantId;
                    user.IsEmailConfirmed = false;

                    await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

                    CheckErrors(await _userManager.CreateAsync(user, password));

                    if (item.RoleNames.IsNullOrEmpty())
                    {
                        CheckErrors(await _userManager.SetRolesAsync(user, item.RoleNames));
                    }
                }
                else
                {
                    var user = await _userManager.GetUserByIdAsync(item.Id);

                    ObjectMapper.Map(item, user);
                    user.SetNormalizedNames();

                    CheckErrors(await _userManager.UpdateAsync(user));

                    if (item.RoleNames.IsNullOrEmpty())
                    {
                        CheckErrors(await _userManager.SetRolesAsync(user, item.RoleNames));
                    }
                }

            }

            CurrentUnitOfWork.SaveChanges();
            return;
        }

        private bool CheckIsEmailConfirmed(UserDto input, User user)
        {
            var result = false;
            // if not IsEmailConfirmed, then continue.
            if (user != null && user.IsEmailConfirmed == true)
            {
                // if not change username or email, then keep IsEmailConfirmed value
                if (input.UserName == user.UserName && input.EmailAddress == user.EmailAddress)
                {
                    result = true;
                }
            }
            return result;
        }
    }
}


