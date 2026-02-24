using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using AppNotification = FollowUp.Core.Entities.Notification;

namespace FollowUp.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly ILogger<NotificationService> _logger;
    private readonly bool _fcmEnabled;

    public NotificationService(
        INotificationRepository notifications,
        IUserRepository users,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<NotificationService> logger)
    {
        _notifications = notifications;
        _users = users;
        _logger = logger;

        // setup firebase if we have the credentials file
        var credentialsPath = configuration["Firebase:CredentialsPath"];
        if (!string.IsNullOrEmpty(credentialsPath))
        {
            var fullPath = Path.IsPathRooted(credentialsPath)
                ? credentialsPath
                : Path.Combine(environment.ContentRootPath, credentialsPath);

            if (File.Exists(fullPath))
            {
                try
                {
                    if (FirebaseApp.DefaultInstance == null)
                    {
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromFile(fullPath)
                        });
                    }
                    _fcmEnabled = true;
                    _logger.LogInformation("Firebase Cloud Messaging initialized successfully from {Path}", fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Firebase. Push notifications will be disabled");
                    _fcmEnabled = false;
                }
            }
            else
            {
                _logger.LogWarning("Firebase credentials file not found at: {Path}", fullPath);
                _fcmEnabled = false;
            }
        }
        else
        {
            _logger.LogWarning("Firebase credentials path not configured");
            _fcmEnabled = false;
        }
    }

    // ─── Private helpers to eliminate duplicate notification boilerplate ────────

    // create, save, and push a notification to a single user
    // returns false if the user was not found
    private async Task<bool> SendToUserAsync(
        int userId,
        string title,
        string body,
        NotificationType type,
        string? payloadJson = null,
        Dictionary<string, string>? pushData = null)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Cannot send {Type} notification - User {UserId} not found", type, userId);
            return false;
        }

        var notification = new AppNotification
        {
            UserId = userId,
            MunicipalityId = user.MunicipalityId,
            Title = title,
            Message = body,
            Type = type,
            IsRead = false,
            IsSent = true,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            PayloadJson = payloadJson
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();
        await SendPushNotificationAsync(userId, title, body, pushData);
        return true;
    }

    // broadcast a notification to all supervisors
    // batches the DB save outside the loop for efficiency
    private async Task SendToAllSupervisorsAsync(
        string title,
        string body,
        NotificationType type,
        string? payloadJson = null,
        Dictionary<string, string>? pushData = null)
    {
        var supervisors = await _users.GetByRoleAsync(UserRole.Supervisor);

        foreach (var supervisor in supervisors)
        {
            var notification = new AppNotification
            {
                UserId = supervisor.UserId,
                MunicipalityId = supervisor.MunicipalityId,
                Title = title,
                Message = body,
                Type = type,
                IsRead = false,
                IsSent = true,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                PayloadJson = payloadJson
            };

            await _notifications.AddAsync(notification);
            await SendPushNotificationAsync(supervisor.UserId, title, body, pushData);
        }

        await _notifications.SaveChangesAsync();
    }

    // ─── Public notification methods ──────────────────────────────────────────

    public async Task SendTaskAssignedNotificationAsync(int userId, int taskId, string taskTitle)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId });
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_assigned" } };

        if (await SendToUserAsync(userId, "مهمة جديدة", $"تم تكليفك بمهمة جديدة: {taskTitle}",
                NotificationType.TaskAssigned, payload, data))
            _logger.LogInformation("Task assigned notification sent to user {UserId} for task {TaskId}", userId, taskId);
    }

    public async Task SendTaskStartedNotificationAsync(int supervisorId, int taskId, string taskTitle, string workerName)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId });
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_started" } };

        if (await SendToUserAsync(supervisorId, "بدء تنفيذ مهمة", $"بدأ العامل {workerName} بتنفيذ المهمة: {taskTitle}",
                NotificationType.TaskStatusChanged, payload, data))
            _logger.LogInformation("Task started notification sent to supervisor {SupervisorId} for task {TaskId}", supervisorId, taskId);
    }

    public async Task SendTaskUpdatedNotificationAsync(int userId, int taskId, string taskTitle)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId });
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_updated" } };

        if (await SendToUserAsync(userId, "تحديث المهمة", $"تم تحديث المهمة: {taskTitle}",
                NotificationType.TaskUpdated, payload, data))
            _logger.LogInformation("Task updated notification sent to user {UserId} for task {TaskId}", userId, taskId);
    }

    public async Task SendIssueReviewedNotificationAsync(int userId, int issueId, string status)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { issueId, status });
        var data = new Dictionary<string, string> { { "issueId", issueId.ToString() }, { "type", "issue_reviewed" } };

        if (await SendToUserAsync(userId, "تحديث البلاغ", $"تم تحديث حالة البلاغ إلى: {status}",
                NotificationType.IssueReviewed, payload, data))
            _logger.LogInformation("Issue reviewed notification sent to user {UserId} for issue {IssueId}", userId, issueId);
    }

    public async Task SendTaskCompletedToSupervisorsAsync(int taskId, string taskTitle, string workerName)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId, workerName });
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_completed" } };

        await SendToAllSupervisorsAsync("مهمة مكتملة بانتظار المراجعة", $"أكمل العامل {workerName} المهمة: {taskTitle}",
            NotificationType.TaskStatusChanged, payload, data);
        _logger.LogInformation("Task completed notification sent to supervisors for task {TaskId} by {WorkerName}", taskId, workerName);
    }

    public async Task SendIssueReportedToSupervisorsAsync(int issueId, string issueTitle, string workerName, string severity)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { issueId, workerName, severity });
        var data = new Dictionary<string, string> { { "issueId", issueId.ToString() }, { "type", "issue_reported" }, { "severity", severity } };

        await SendToAllSupervisorsAsync("بلاغ جديد", $"أبلغ العامل {workerName} عن مشكلة: {issueTitle} (الخطورة: {severity})",
            NotificationType.IssueReported, payload, data);
        _logger.LogInformation("Issue reported notification sent to supervisors for issue {IssueId} by {WorkerName}", issueId, workerName);
    }

    public async Task SendSystemAlertAsync(int userId, string message)
    {
        var data = new Dictionary<string, string> { { "type", "system_alert" } };

        if (await SendToUserAsync(userId, "تنبيه النظام", message, NotificationType.SystemAlert, null, data))
            _logger.LogInformation("System alert sent to user {UserId}", userId);
    }

    public async Task SendBatteryLowNotificationAsync(int workerId, string workerName, int batteryLevel)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { workerId, batteryLevel });
        var data = new Dictionary<string, string> { { "workerId", workerId.ToString() }, { "batteryLevel", batteryLevel.ToString() }, { "type", "battery_low" } };

        await SendToAllSupervisorsAsync("تنبيه بطارية منخفضة",
            $"بطارية العامل {workerName} منخفضة ({batteryLevel}%). قد لا يتمكن من إكمال مهامه.",
            NotificationType.BatteryLow, payload, data);
        _logger.LogInformation("Battery low notification sent for worker {WorkerId} ({BatteryLevel}%)", workerId, batteryLevel);
    }

    public async Task SendTaskAutoRejectedToWorkerAsync(int workerId, int taskId, string taskTitle, string reason, int distanceMeters)
    {
        var body = $"تم رفض إثبات المهمة \"{taskTitle}\" تلقائياً.\n{reason}\nالمسافة من موقع المهمة: {distanceMeters} متر";
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId, reason, distanceMeters });
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_auto_rejected" }, { "distanceMeters", distanceMeters.ToString() } };

        if (await SendToUserAsync(workerId, "تم رفض إثبات المهمة", body, NotificationType.TaskStatusChanged, payload, data))
            _logger.LogInformation("Task auto-rejected notification sent to worker {WorkerId} for task {TaskId}", workerId, taskId);
    }

    public async Task SendTaskAutoRejectedToSupervisorsAsync(int taskId, string taskTitle, string workerName, string reason, int distanceMeters)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId, workerName, reason, distanceMeters });
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_auto_rejected_supervisor" }, { "distanceMeters", distanceMeters.ToString() } };

        await SendToAllSupervisorsAsync("رفض تلقائي لإثبات مهمة",
            $"تم رفض إثبات العامل {workerName} للمهمة \"{taskTitle}\" تلقائياً.\n{reason}\nالمسافة: {distanceMeters} متر",
            NotificationType.SystemAlert, payload, data);
        _logger.LogInformation("Task auto-rejected notification sent to supervisors for task {TaskId}", taskId);
    }

    public async Task SendWarningIssuedToWorkerAsync(int workerId, string reason, int totalWarnings)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { reason, totalWarnings });
        var data = new Dictionary<string, string> { { "type", "warning_issued" }, { "totalWarnings", totalWarnings.ToString() } };

        if (await SendToUserAsync(workerId, "تحذير", $"تم إصدار تحذير: {reason}\nإجمالي التحذيرات: {totalWarnings}",
                NotificationType.SystemAlert, payload, data))
            _logger.LogInformation("Warning notification sent to worker {WorkerId}, total warnings: {TotalWarnings}", workerId, totalWarnings);
    }

    public async Task SendWarningAlertToSupervisorsAsync(int workerId, string workerName, string reason, int totalWarnings)
    {
        var title = totalWarnings >= 3 ? "عامل وصل للحد الأقصى من التحذيرات" : "تحذير لعامل";
        var payload = System.Text.Json.JsonSerializer.Serialize(new { workerId, workerName, reason, totalWarnings });
        var data = new Dictionary<string, string> { { "workerId", workerId.ToString() }, { "type", "worker_warning_alert" }, { "totalWarnings", totalWarnings.ToString() } };

        await SendToAllSupervisorsAsync(title, $"تم إصدار تحذير للعامل {workerName}.\nالسبب: {reason}\nإجمالي التحذيرات: {totalWarnings}",
            NotificationType.SystemAlert, payload, data);
        _logger.LogInformation("Warning alert sent to supervisors for worker {WorkerId}, total warnings: {TotalWarnings}", workerId, totalWarnings);
    }

    public async Task SendTaskExtensionRequestAsync(int supervisorId, int taskId, string taskTitle, DateTime originalDeadline, DateTime requestedDeadline)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId, originalDeadline, requestedDeadline });
        var data = new Dictionary<string, string> { { "type", "task_extension_request" }, { "taskId", taskId.ToString() } };

        if (await SendToUserAsync(supervisorId, "طلب تمديد موعد مهمة",
                $"تم طلب تمديد موعد المهمة \"{taskTitle}\" من {originalDeadline:dd/MM/yyyy} إلى {requestedDeadline:dd/MM/yyyy}",
                NotificationType.TaskUpdated, payload, data))
            _logger.LogInformation("Extension request notification sent to supervisor {SupervisorId} for task {TaskId}", supervisorId, taskId);
    }

    public async Task SendTaskMilestoneNotificationAsync(int workerId, int taskId, string taskTitle, int milestone)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId, milestone });
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_milestone" }, { "milestone", milestone.ToString() } };

        if (await SendToUserAsync(workerId, $"تم إنجاز {milestone}% من المهمة",
                $"أحسنت! لقد أنجزت {milestone}% من المهمة \"{taskTitle}\"",
                NotificationType.TaskUpdated, payload, data))
            _logger.LogInformation("Task milestone notification sent to worker {WorkerId} for task {TaskId} at {Milestone}%", workerId, taskId, milestone);
    }

    public async Task SendManualAttendanceApprovedAsync(int workerId, int attendanceId)
    {
        var data = new Dictionary<string, string> { { "type", "manual_attendance_approved" }, { "attendanceId", attendanceId.ToString() } };

        if (await SendToUserAsync(workerId, "تمت الموافقة على الحضور",
                "تمت الموافقة على طلب تسجيل الحضور اليدوي الخاص بك",
                NotificationType.ManualAttendanceApproved, null, data))
            _logger.LogInformation("Manual attendance approved notification sent to worker {WorkerId}", workerId);
    }

    public async Task SendManualAttendanceRejectedAsync(int workerId, int attendanceId, string reason)
    {
        var data = new Dictionary<string, string> { { "type", "manual_attendance_rejected" }, { "attendanceId", attendanceId.ToString() } };

        if (await SendToUserAsync(workerId, "تم رفض طلب الحضور",
                $"تم رفض طلب تسجيل الحضور اليدوي: {reason}",
                NotificationType.ManualAttendanceRejected, null, data))
            _logger.LogInformation("Manual attendance rejected notification sent to worker {WorkerId}", workerId);
    }

    public async Task SendAppealSubmittedToSupervisorsAsync(int taskId, string taskTitle, string workerName)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { taskId });
        var data = new Dictionary<string, string> { { "type", "appeal_submitted" }, { "taskId", taskId.ToString() } };

        await SendToAllSupervisorsAsync("طعن جديد على مهمة",
            $"قدّم العامل {workerName} طعناً على المهمة \"{taskTitle}\"",
            NotificationType.AppealSubmitted, payload, data);
        _logger.LogInformation("Appeal submitted notification sent to supervisors for task {TaskId}", taskId);
    }

    // ─── Push notification via Firebase ───────────────────────────────────────

    private async Task SendPushNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_fcmEnabled)
        {
            _logger.LogDebug("FCM disabled, skipping push notification for user {UserId}", userId);
            return;
        }

        try
        {
            var user = await _users.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.FcmToken))
            {
                _logger.LogDebug("User {UserId} has no FCM token, skipping push notification", userId);
                return;
            }

            var message = new Message
            {
                Token = user.FcmToken,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                    }
                },
                Data = data
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("FCM push notification sent to user {UserId}, response: {Response}", userId, response);
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            _logger.LogWarning("FCM token invalid for user {UserId}, clearing token", userId);
            var user = await _users.GetByIdAsync(userId);
            if (user != null)
            {
                user.FcmToken = null;
                await _users.UpdateAsync(user);
                await _users.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM push notification to user {UserId}", userId);
        }
    }
}
