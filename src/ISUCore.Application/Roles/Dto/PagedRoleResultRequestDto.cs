using Abp.Application.Services.Dto;

namespace ISUCore.Roles.Dto
{
    public class PagedRoleResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
        public string Sorting { get; set; }
        public bool Descending { get; set; }
    }
}


