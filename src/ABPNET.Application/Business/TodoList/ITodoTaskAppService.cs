using Abp.Application.Services;
using ABPNET.Business.TodoList.Dto;
namespace ABPNET.Business.TodoList
{
    public interface ITodoTaskAppService : IAsyncCrudAppService<TodoTaskDto, long, PagedTodoTaskResultRequestDto, CreateTodoTaskDto, TodoTaskDto>
    {
        void MarkAsCompleted(long id);
    }
}



