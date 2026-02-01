using FollowUp.Core.Enums;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Services;

public interface INotificationService
{
    Task SendTaskAssignedNotificationAsync(int userId, int taskId, string taskTitle);
    Task SendTaskUpdatedNotificationAsync(int userId, int taskId, string taskTitle);
    Task SendTaskCompletedToSupervisorsAsync(int taskId, string taskTitle, string workerName);
    Task SendIssueReportedToSupervisorsAsync(int issueId, string issueTitle, string workerName, string severity);
    Task SendIssueReviewedNotificationAsync(int userId, int issueId, string status);
    Task SendSystemAlertAsync(int userId, string message);
    Task SendBatteryLowNotificationAsync(int workerId, string workerName, int batteryLevel);

    // Task rejection notifications
    Task SendTaskAutoRejectedToWorkerAsync(int workerId, int taskId, string taskTitle, string reason, int distanceMeters);
    Task SendTaskAutoRejectedToSupervisorsAsync(int taskId, string taskTitle, string workerName, string reason, int distanceMeters);

    // Warning system notifications
    Task SendWarningIssuedToWorkerAsync(int workerId, string reason, int totalWarnings);
    Task SendWarningAlertToSupervisorsAsync(int workerId, string workerName, string reason, int totalWarnings);

    // Task extension request notification
    Task SendTaskExtensionRequestAsync(int supervisorId, int taskId, string taskTitle, DateTime originalDeadline, DateTime requestedDeadline);
}
