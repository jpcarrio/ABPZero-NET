using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using ISUCore.Models;
namespace ISUCore.Business.TodoList.Dto
{
    [AutoMapTo(typeof(TodoTask))]
    public class CreateTodoTaskDto
    {
        [Required]
        [MaxLength(TodoTaskBase.MaxNameLength)]
        public string Name { get; set; }
        public TodoTaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public long? CreatorUserId { get; set; }
        public virtual long UserId { get; set; }
    }
}

