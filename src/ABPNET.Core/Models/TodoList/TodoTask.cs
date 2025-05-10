using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;

namespace ABPNET.Models
{
    [Table("AppTask")]
    public class TodoTask : Entity<long>, ICreationAudited
    {

        public TodoTask()
        {
            CreationTime = Clock.Now;
            Priority = TodoTaskPriority.Low;
        }

        [Required]
        [StringLength(TodoTaskBase.MaxNameLength)]
        public string Name { get; set; }
        public TodoTaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreationTime { get; set; }
        public long? CreatorUserId { get; set; }
        public virtual long UserId { get; set; }
    }

    public enum TodoTaskPriority : byte
    {
        High = 0,
        Medium = 1,
        Low = 2
    }

    public class TodoTaskBase
    {
        public const int MaxNameLength = 256;
    }
}



