using System.Threading.Tasks;
using Abp.Application.Services;
using ABPNET.Authorization.Accounts.Dto;

namespace ABPNET.Authorization.Accounts
{
    public interface IAccountAppService : IApplicationService
    {
        Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

        Task<RegisterOutput> Register(RegisterInput input);
    }
}



