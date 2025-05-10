using Abp.Application.Services.Dto;
namespace ABPNET.Business.TodoList.Dto
{
    public class PagedTodoTaskResultRequestDto : PagedResultRequestDto, ISortedResultRequest
    {
        public string Keyword { get; set; }
        public bool? IsCompleted { get; set; }
        public long UserId { get; set; }
        public string Sorting { get; set; }
        public bool Descending { get; set; }
    }
}



