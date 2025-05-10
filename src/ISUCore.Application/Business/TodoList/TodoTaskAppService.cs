using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using ISUCore.Authorization;
using ISUCore.Authorization.Roles;
using ISUCore.Authorization.Users;
using ISUCore.Business.TodoList.Dto;
using ISUCore.Models;
namespace ISUCore.Business.TodoList
{
    [AbpAuthorize(PermissionNames.Pages_Tasks)]
    public class TodoTaskAppService : AsyncCrudAppService<TodoTask, TodoTaskDto, long, PagedTodoTaskResultRequestDto, CreateTodoTaskDto, TodoTaskDto>, ITodoTaskAppService
    {
        private readonly IRepository<TodoTask, long> _taskRepository;
        private readonly IAbpSession _abpSession;
        private readonly RoleManager _roleManager;
        private readonly UserManager _userManager;
        public TodoTaskAppService(IRepository<TodoTask, long> repository, IAbpSession abpSession, RoleManager roleManager, UserManager userManager) : base(repository)
        {
            _taskRepository = repository;
            _abpSession = abpSession;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public override async Task<TodoTaskDto> CreateAsync(CreateTodoTaskDto input)
        {
            CheckCreatePermission();
            var task = ObjectMapper.Map<TodoTask>(input);
            task.CreatorUserId = _abpSession.UserId;
            task.UserId = _abpSession.UserId.Value;
            task = await _taskRepository.InsertAsync(task);
            CurrentUnitOfWork.SaveChanges();
            return MapToEntityDto(task);
        }
        public override async Task<TodoTaskDto> UpdateAsync(TodoTaskDto input)
        {
            CheckUpdatePermission();
            var task = await _taskRepository.GetAsync(input.Id);
            task.Name = input.Name;
            task.Priority = input.Priority;
            task.DueDate = input.DueDate;
            task.ReminderDate = input.ReminderDate;
            task.IsCompleted = input.IsCompleted;
            await _taskRepository.UpdateAsync(task);
            return MapToEntityDto(task);
        }
        protected override IQueryable<TodoTask> CreateFilteredQuery(PagedTodoTaskResultRequestDto input)
        {
            var keyword = input?.Keyword?.ToLower();
            var isAdminUser = _userManager.IsInRoleAsync(_userManager.GetUserById(_abpSession.UserId ?? 0), "Admin").Result && _userManager.GetUserById(_abpSession.UserId ?? 0).UserName == "admin";
            return Repository.GetAll()
                  .WhereIf(!isAdminUser, x => x.UserId == _abpSession.UserId)
                  .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.Name.ToLower().Contains(keyword)
                           //|| x.Priority.ToString().ToLower().Contains(keyword)
                           || (x.DueDate != null && x.DueDate.ToString().ToLower().Contains(keyword)))
                  .WhereIf(input.IsCompleted.HasValue, x => x.IsCompleted == input.IsCompleted);
        }
        public void MarkAsCompleted(long id)
        {
            var _task = _taskRepository.Get(id);
            _task.IsCompleted = true;
            CurrentUnitOfWork.SaveChanges();
        }
        protected override IQueryable<TodoTask> ApplySorting(IQueryable<TodoTask> query, PagedTodoTaskResultRequestDto input)
        {
            switch (input.Sorting)
            {
                case "TaskName":
                    return input.Descending ? query.OrderByDescending(x => x.Name.ToLower()) : query.OrderBy(x => x.Name.ToLower());
                case "Priority":
                    return input.Descending ? query.OrderByDescending(x => x.Priority) : query.OrderBy(x => x.Priority);
                case "DueDate":
                    return input.Descending ? query.OrderByDescending(x => x.DueDate) : query.OrderBy(x => x.DueDate);
                default:
                    return query.OrderBy(x => x.Name);
            }
        }
    }
}

