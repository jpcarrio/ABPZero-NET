using Abp.Application.Services;
using ABPNET.MultiTenancy.Dto;

namespace ABPNET.MultiTenancy
{
    public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
    {
    }
}




