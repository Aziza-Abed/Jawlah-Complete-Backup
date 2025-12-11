using Jawlah.Core.Enums;
using TaskEntity = Jawlah.Core.Entities.Task;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.Core.Interfaces.Repositories;

public interface ITaskRepository : IRepository<TaskEntity>
{
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetUserTasksAsync(int userId, TaskStatus? status = null);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetZoneTasksAsync(int zoneId, TaskStatus? status = null);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync();
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksDueSoonAsync(int hours);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetFilteredTasksAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate, TaskStatus? status);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksModifiedAfterAsync(int userId, DateTime lastSyncTime);
}
