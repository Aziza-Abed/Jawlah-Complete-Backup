using Jawlah.Core.Enums;
using TaskEntity = Jawlah.Core.Entities.Task;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.Core.Interfaces.Repositories;

public interface ITaskRepository : IRepository<TaskEntity>
{
    Task<IEnumerable<TaskEntity>> GetUserTasksAsync(int userId, TaskStatus? status = null, TaskPriority? priority = null, int page = 1, int pageSize = 50);
    Task<IEnumerable<TaskEntity>> GetZoneTasksAsync(int zoneId, TaskStatus? status = null);

    // get overdue tasks, optionally filter by user
    Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(int? userId = null);

    Task<IEnumerable<TaskEntity>> GetTasksDueSoonAsync(int hours);
    Task<IEnumerable<TaskEntity>> GetFilteredTasksAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate, TaskStatus? status);
    Task<IEnumerable<TaskEntity>> GetTasksModifiedAfterAsync(int userId, DateTime lastSyncTime);
}
