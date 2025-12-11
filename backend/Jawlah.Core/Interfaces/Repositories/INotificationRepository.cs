using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    System.Threading.Tasks.Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    System.Threading.Tasks.Task<int> GetUnreadCountAsync(int userId);
    System.Threading.Tasks.Task MarkAsReadAsync(int notificationId);
    System.Threading.Tasks.Task MarkAllAsReadAsync(int userId);
    System.Threading.Tasks.Task<IEnumerable<Notification>> GetNotificationsCreatedAfterAsync(int userId, DateTime lastSyncTime);
}
