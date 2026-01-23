using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace FollowUp.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _dbSet
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _dbSet.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }
    }

    public async Task<IEnumerable<Notification>> GetNotificationsCreatedAfterAsync(int userId, DateTime lastSyncTime)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.CreatedAt > lastSyncTime)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}
