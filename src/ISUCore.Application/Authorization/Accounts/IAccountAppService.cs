using System.Threading.Tasks;
using Abp.Application.Services;
using ISUCore.Authorization.Accounts.Dto;

namespace ISUCore.Authorization.Accounts
{
    public interface IAccountAppService : IApplicationService
    {
        Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

        Task<RegisterOutput> Register(RegisterInput input);
    }
}

