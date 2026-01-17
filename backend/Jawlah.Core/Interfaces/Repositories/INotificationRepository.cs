using Jawlah.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Core.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task<IEnumerable<Notification>> GetNotificationsCreatedAfterAsync(int userId, DateTime lastSyncTime);
}
