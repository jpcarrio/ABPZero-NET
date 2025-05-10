using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using ABPNET.Roles.Dto;
using ABPNET.Users.Dto;

namespace ABPNET.Users
{
    public interface IUserAppService : IAsyncCrudAppService<UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>
    {
        Task DeActivate(EntityDto<long> user);
        Task Activate(EntityDto<long> user);
        Task<ListResultDto<RoleDto>> GetRoles();
        Task ChangeLanguage(ChangeUserLanguageDto input);

        //Task<bool> ChangePassword(ChangePasswordDto input);

        Task<bool> ChangeUserImage(ChangeUserImageDto input);

        Task<bool> ChangeUserAccountActivation(long id);

        bool IsConnectionAlive();
    }
}



