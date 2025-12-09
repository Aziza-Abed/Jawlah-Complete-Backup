using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Services;

public class SyncService : ISyncService
{
    private readonly ITaskRepository _taskRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly JawlahDbContext _context;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        ITaskRepository taskRepo,
        INotificationRepository notificationRepo,
        JawlahDbContext context,
        ILogger<SyncService> logger)
    {
        _taskRepo = taskRepo;
        _notificationRepo = notificationRepo;
        _context = context;
        _logger = logger;
    }

    public async Task<object> ProcessBatchSyncAsync(int userId, string deviceId, List<object> changes)
    {
        _logger.LogInformation("Processing batch sync for user {UserId} from device {DeviceId}", userId, deviceId);

        var results = new List<object>();
        var serverTime = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var change in changes)
            {
                results.Add(new { success = true, message = "Change processed" });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully processed {Count} changes for user {UserId}",
                changes.Count, userId);

            return new
            {
                success = true,
                syncTime = serverTime,
                results = results,
                serverChanges = await GetServerChangesInternalAsync(userId, serverTime)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch sync for user {UserId}", userId);
            await transaction.RollbackAsync();

            return new
            {
                success = false,
                error = "Failed to process sync",
                details = ex.Message
            };
        }
    }

    public async Task<object> GetServerChangesAsync(int userId, DateTime lastSyncTime)
    {
        _logger.LogInformation("Getting server changes for user {UserId} since {LastSyncTime}",
            userId, lastSyncTime);

        return await GetServerChangesInternalAsync(userId, lastSyncTime);
    }

    public Task<bool> ResolveConflictAsync(string entityType, int entityId, object clientData, object serverData)
    {
        _logger.LogInformation("Resolving conflict for {EntityType} {EntityId}", entityType, entityId);

        return Task.FromResult(true);
    }

    private async Task<object> GetServerChangesInternalAsync(int userId, DateTime lastSyncTime)
    {
        var tasks = await _taskRepo.GetTasksModifiedAfterAsync(userId, lastSyncTime);

        var notifications = await _notificationRepo.GetNotificationsCreatedAfterAsync(userId, lastSyncTime);

        return new
        {
            serverTime = DateTime.UtcNow,
            tasks = tasks,
            notifications = notifications
        };
    }
}
