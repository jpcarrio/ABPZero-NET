using System.Threading.Tasks;
using Abp.Application.Services;
using ISUCore.Sessions.Dto;

namespace ISUCore.Sessions
{
    public interface ISessionAppService : IApplicationService
    {
        Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
    }
}

