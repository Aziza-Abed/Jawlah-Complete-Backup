using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TaskEntity = Jawlah.Core.Entities.Task;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.Infrastructure.Repositories;

public class TaskRepository : Repository<TaskEntity>, ITaskRepository
{
    public TaskRepository(JawlahDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetUserTasksAsync(int userId, TaskStatus? status = null)
    {
        var query = _dbSet
            .Include(t => t.Zone)
            .Include(t => t.AssignedByUser)
            .Where(t => t.AssignedToUserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetZoneTasksAsync(int zoneId, TaskStatus? status = null)
    {
        var query = _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Where(t => t.ZoneId == zoneId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.Zone)
            .Where(t => t.DueDate < now &&
                       t.Status != TaskStatus.Completed &&
                       t.Status != TaskStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksDueSoonAsync(int hours)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddHours(hours);

        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.Zone)
            .Where(t => t.DueDate >= now &&
                       t.DueDate <= threshold &&
                       t.Status == TaskStatus.Pending)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetFilteredTasksAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate, TaskStatus? status)
    {
        var query = _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Zone)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(t => t.AssignedToUserId == userId.Value);

        if (zoneId.HasValue)
            query = query.Where(t => t.ZoneId == zoneId.Value);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksModifiedAfterAsync(int userId, DateTime lastSyncTime)
    {
        return await _dbSet
            .Where(t => t.AssignedToUserId == userId && t.SyncTime > lastSyncTime)
            .ToListAsync();
    }
}
