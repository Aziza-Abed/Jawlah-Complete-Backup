using FollowUp.Core.Enums;
using TaskEntity = FollowUp.Core.Entities.Task;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;

namespace FollowUp.Core.Interfaces.Repositories;

public interface ITaskRepository : IRepository<TaskEntity>
{
    Task<IEnumerable<TaskEntity>> GetUserTasksAsync(int userId, TaskStatus? status = null, TaskPriority? priority = null, int page = 1, int pageSize = 50);
    Task<IEnumerable<TaskEntity>> GetZoneTasksAsync(int zoneId, TaskStatus? status = null);

    // get overdue tasks, optionally filter by user
    Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(int? userId = null);

    Task<IEnumerable<TaskEntity>> GetTasksDueSoonAsync(int hours);
    Task<IEnumerable<TaskEntity>> GetFilteredTasksAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate, TaskStatus? status);
    Task<IEnumerable<TaskEntity>> GetTasksModifiedAfterAsync(int userId, DateTime lastSyncTime);

    // Dashboard optimized methods - use database-level aggregation
    Task<TaskStatsDto> GetTaskStatsAsync(IEnumerable<int> workerIds, DateTime today);
    Task<IEnumerable<TaskEntity>> GetTasksForWorkersAsync(IEnumerable<int> workerIds);
}

// DTO for dashboard task stats (avoids loading all entities)
public class TaskStatsDto
{
    public int CreatedToday { get; set; }
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int CompletedToday { get; set; }
}
