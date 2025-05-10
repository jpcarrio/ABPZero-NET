using System.Collections.Generic;
using ABPNET.Authorization.Roles;

namespace ABPNET.Configuration.Dto;

public class RemindersDto
{
    public string employeeHoursWeeklyReminders { get; set; }
    public string employeeHoursMonthlyReminders { get; set; }
    public int? ScheduledByTenantId { get; set; }
    public string RolesIds { get; set; }
}



