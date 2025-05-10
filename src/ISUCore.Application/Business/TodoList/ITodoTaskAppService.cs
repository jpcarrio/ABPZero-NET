using Abp.Application.Services;
using ISUCore.Business.TodoList.Dto;
namespace ISUCore.Business.TodoList
{
    public interface ITodoTaskAppService : IAsyncCrudAppService<TodoTaskDto, long, PagedTodoTaskResultRequestDto, CreateTodoTaskDto, TodoTaskDto>
    {
        void MarkAsCompleted(long id);
    }
}

