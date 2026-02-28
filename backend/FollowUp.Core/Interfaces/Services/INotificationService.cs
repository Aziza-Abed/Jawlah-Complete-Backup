using FollowUp.Core.Enums;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Services;

public interface INotificationService
{
    Task SendTaskAssignedNotificationAsync(int userId, int taskId, string taskTitle);
    Task SendTaskStartedNotificationAsync(int supervisorId, int taskId, string taskTitle, string workerName);
    Task SendTaskUpdatedNotificationAsync(int userId, int taskId, string taskTitle);
    Task SendTaskCompletedToSupervisorsAsync(int taskId, string taskTitle, string workerName, int? municipalityId = null);
    Task SendIssueReportedToSupervisorsAsync(int issueId, string issueTitle, string workerName, string severity, int? municipalityId = null);
    Task SendIssueReviewedNotificationAsync(int userId, int issueId, string status);
    Task SendSystemAlertAsync(int userId, string message);
    Task SendBatteryLowNotificationAsync(int workerId, string workerName, int batteryLevel, int? municipalityId = null);

    // Task rejection notifications
    Task SendTaskAutoRejectedToWorkerAsync(int workerId, int taskId, string taskTitle, string reason, int distanceMeters);
    Task SendTaskAutoRejectedToSupervisorsAsync(int taskId, string taskTitle, string workerName, string reason, int distanceMeters, int? municipalityId = null);

    // Warning system notifications
    Task SendWarningIssuedToWorkerAsync(int workerId, string reason, int totalWarnings);
    Task SendWarningAlertToSupervisorsAsync(int workerId, string workerName, string reason, int totalWarnings, int? municipalityId = null);

    // Task extension request notification
    Task SendTaskExtensionRequestAsync(int supervisorId, int taskId, string taskTitle, DateTime originalDeadline, DateTime requestedDeadline);

    // Task milestone notification (25%, 50%, 75% progress)
    Task SendTaskMilestoneNotificationAsync(int workerId, int taskId, string taskTitle, int milestone);

    // Manual attendance approval/rejection notifications
    Task SendManualAttendanceApprovedAsync(int workerId, int attendanceId);
    Task SendManualAttendanceRejectedAsync(int workerId, int attendanceId, string reason);

    // Appeal notification
    Task SendAppealSubmittedToSupervisorsAsync(int taskId, string taskTitle, string workerName, int? municipalityId = null);
}
