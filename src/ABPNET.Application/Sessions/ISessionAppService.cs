using System.Threading.Tasks;
using Abp.Application.Services;
using ABPNET.Sessions.Dto;

namespace ABPNET.Sessions
{
    public interface ISessionAppService : IApplicationService
    {
        Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
    }
}



