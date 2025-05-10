using Abp.Application.Services.Dto;

namespace ABPNET.Users.Dto
{
    //custom PagedResultRequestDto
    public class PagedUserResultRequestDto : PagedResultRequestDto
    {
        public string Keyword { get; set; }
        public string Sorting { get; set; }
        public bool? IsActive { get; set; }
        public bool Descending { get; set; }
    }
}



