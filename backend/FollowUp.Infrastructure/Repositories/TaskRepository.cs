using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskEntity = FollowUp.Core.Entities.Task;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;

namespace FollowUp.Infrastructure.Repositories;

public class TaskRepository : Repository<TaskEntity>, ITaskRepository
{
    public TaskRepository(FollowUpDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<TaskEntity>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Zone)
            .Include(t => t.Photos)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public override async Task<TaskEntity?> GetByIdAsync(int id)
    {
        // load the task with all its related data like user and zone
        return await _dbSet
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Zone)
            .Include(t => t.Photos)
            .FirstOrDefaultAsync(t => t.TaskId == id);
    }

    public async Task<IEnumerable<TaskEntity>> GetUserTasksAsync(int userId, TaskStatus? status = null, TaskPriority? priority = null, int page = 1, int pageSize = 50)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(t => t.Zone)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Photos)
            .Where(t => t.AssignedToUserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetZoneTasksAsync(int zoneId, TaskStatus? status = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Photos)
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

    public async Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync(int? userId = null)
    {
        var now = DateTime.UtcNow;

        var query = _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Include(t => t.Zone)
            .Include(t => t.Photos)
            .Where(t => t.DueDate < now &&
                       t.Status != TaskStatus.Completed &&
                       t.Status != TaskStatus.Cancelled);

        if (userId.HasValue)
        {
            query = query.Where(t => t.AssignedToUserId == userId.Value);
        }

        return await query
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksDueSoonAsync(int hours)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddHours(hours);

        return await _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Include(t => t.Zone)
            .Include(t => t.Photos)
            .Where(t => t.DueDate >= now &&
                       t.DueDate <= threshold &&
                       t.Status == TaskStatus.Pending)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskEntity>> GetFilteredTasksAsync(int? userId, int? zoneId, DateTime? fromDate, DateTime? toDate, TaskStatus? status)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Zone)
            .Include(t => t.Photos)
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

    public async Task<IEnumerable<TaskEntity>> GetTasksModifiedAfterAsync(int userId, DateTime lastSyncTime)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Zone)
            .Include(t => t.Photos)
            .Where(t => t.AssignedToUserId == userId && t.SyncTime > lastSyncTime)
            .ToListAsync();
    }

    // Dashboard optimized: Get stats using database-level COUNT instead of loading entities
    public async Task<TaskStatsDto> GetTaskStatsAsync(IEnumerable<int> workerIds, DateTime today)
    {
        var workerIdSet = workerIds.ToList();
        var tomorrow = today.AddDays(1);

        // Single query with conditional aggregation - much faster than loading all entities
        var stats = await _dbSet
            .AsNoTracking()
            .Where(t => workerIdSet.Contains(t.AssignedToUserId))
            .GroupBy(t => 1) // Group all into one
            .Select(g => new TaskStatsDto
            {
                CreatedToday = g.Count(t => t.CreatedAt >= today && t.CreatedAt < tomorrow),
                Pending = g.Count(t => t.Status == TaskStatus.Pending),
                InProgress = g.Count(t => t.Status == TaskStatus.InProgress),
                CompletedToday = g.Count(t => t.Status == TaskStatus.Completed && t.CompletedAt >= today)
            })
            .FirstOrDefaultAsync();

        return stats ?? new TaskStatsDto();
    }

    // Get tasks for workers (for worker-status endpoint) with minimal includes
    public async Task<IEnumerable<TaskEntity>> GetTasksForWorkersAsync(IEnumerable<int> workerIds)
    {
        var workerIdSet = workerIds.ToList();

        return await _dbSet
            .AsNoTracking()
            .Where(t => workerIdSet.Contains(t.AssignedToUserId))
            .Select(t => new TaskEntity
            {
                TaskId = t.TaskId,
                AssignedToUserId = t.AssignedToUserId,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }
}
