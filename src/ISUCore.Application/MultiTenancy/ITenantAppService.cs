using Abp.Application.Services;
using ISUCore.MultiTenancy.Dto;

namespace ISUCore.MultiTenancy
{
    public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
    {
    }
}


