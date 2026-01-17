using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using AppNotification = Jawlah.Core.Entities.Notification;

namespace Jawlah.Infrastructure.Services;

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
            // resolve path relative to app directory
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

    public async Task SendTaskAssignedNotificationAsync(int userId, int taskId, string taskTitle)
    {
        var title = "مهمة جديدة";
        var body = $"تم تكليفك بمهمة جديدة: {taskTitle}";
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_assigned" } };

        // get user to get their municipality
        var user = await _users.GetByIdAsync(userId);

        var notification = new AppNotification
        {
            UserId = userId,
            MunicipalityId = user?.MunicipalityId ?? 0,
            Title = title,
            Message = body,
            Type = NotificationType.TaskAssigned,
            IsRead = false,
            IsSent = true,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { taskId })
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        // also send push notif to phone
        await SendPushNotificationAsync(userId, title, body, data);

        _logger.LogInformation("Task assigned notification sent to user {UserId} for task {TaskId}", userId, taskId);
    }

    public async Task SendTaskUpdatedNotificationAsync(int userId, int taskId, string taskTitle)
    {
        var title = "تحديث المهمة";
        var body = $"تم تحديث المهمة: {taskTitle}";
        var data = new Dictionary<string, string> { { "taskId", taskId.ToString() }, { "type", "task_updated" } };

        // get user to get their municipality
        var user = await _users.GetByIdAsync(userId);

        var notification = new AppNotification
        {
            UserId = userId,
            MunicipalityId = user?.MunicipalityId ?? 0,
            Title = title,
            Message = body,
            Type = NotificationType.TaskUpdated,
            IsRead = false,
            IsSent = true,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { taskId })
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        // also send push notif to phone
        await SendPushNotificationAsync(userId, title, body, data);

        _logger.LogInformation("Task updated notification sent to user {UserId} for task {TaskId}", userId, taskId);
    }

    public async Task SendIssueReviewedNotificationAsync(int userId, int issueId, string status)
    {
        var title = "تحديث البلاغ";
        var body = $"تم تحديث حالة البلاغ إلى: {status}";
        var data = new Dictionary<string, string> { { "issueId", issueId.ToString() }, { "type", "issue_reviewed" } };

        // get user to get their municipality
        var user = await _users.GetByIdAsync(userId);

        var notification = new AppNotification
        {
            UserId = userId,
            MunicipalityId = user?.MunicipalityId ?? 0,
            Title = title,
            Message = body,
            Type = NotificationType.IssueReviewed,
            IsRead = false,
            IsSent = true,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { issueId, status })
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        // also send push notif to phone
        await SendPushNotificationAsync(userId, title, body, data);

        _logger.LogInformation("Issue reviewed notification sent to user {UserId} for issue {IssueId}", userId, issueId);
    }

    public async Task SendTaskCompletedToSupervisorsAsync(int taskId, string taskTitle, string workerName)
    {
        var title = "مهمة مكتملة بانتظار المراجعة";
        var body = $"أكمل العامل {workerName} المهمة: {taskTitle}";
        var data = new Dictionary<string, string>
        {
            { "taskId", taskId.ToString() },
            { "type", "task_completed" }
        };

        // get all supervisors to notify them
        var supervisors = await _users.GetByRoleAsync(UserRole.Supervisor);

        foreach (var supervisor in supervisors)
        {
            var notification = new AppNotification
            {
                UserId = supervisor.UserId,
                MunicipalityId = supervisor.MunicipalityId,
                Title = title,
                Message = body,
                Type = NotificationType.TaskUpdated,
                IsRead = false,
                IsSent = true,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { taskId, workerName })
            };

            await _notifications.AddAsync(notification);

            // send push notification
            await SendPushNotificationAsync(supervisor.UserId, title, body, data);
        }

        await _notifications.SaveChangesAsync();
        _logger.LogInformation("Task completed notification sent to supervisors for task {TaskId} by {WorkerName}", taskId, workerName);
    }

    public async Task SendIssueReportedToSupervisorsAsync(int issueId, string issueTitle, string workerName, string severity)
    {
        var title = "بلاغ جديد";
        var body = $"أبلغ العامل {workerName} عن مشكلة: {issueTitle} (الخطورة: {severity})";
        var data = new Dictionary<string, string>
        {
            { "issueId", issueId.ToString() },
            { "type", "issue_reported" },
            { "severity", severity }
        };

        // get all supervisors to notify them
        var supervisors = await _users.GetByRoleAsync(UserRole.Supervisor);

        foreach (var supervisor in supervisors)
        {
            var notification = new AppNotification
            {
                UserId = supervisor.UserId,
                MunicipalityId = supervisor.MunicipalityId,
                Title = title,
                Message = body,
                Type = NotificationType.IssueReviewed, // reusing type, could add new type
                IsRead = false,
                IsSent = true,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { issueId, workerName, severity })
            };

            await _notifications.AddAsync(notification);

            // send push notification
            await SendPushNotificationAsync(supervisor.UserId, title, body, data);
        }

        await _notifications.SaveChangesAsync();
        _logger.LogInformation("Issue reported notification sent to supervisors for issue {IssueId} by {WorkerName}", issueId, workerName);
    }

    public async Task SendSystemAlertAsync(int userId, string message)
    {
        var title = "تنبيه النظام";
        var data = new Dictionary<string, string> { { "type", "system_alert" } };

        // get user to get their municipality
        var user = await _users.GetByIdAsync(userId);

        var notification = new AppNotification
        {
            UserId = userId,
            MunicipalityId = user?.MunicipalityId ?? 0,
            Title = title,
            Message = message,
            Type = NotificationType.SystemAlert,
            IsRead = false,
            IsSent = true,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        // also send push notif to phone
        await SendPushNotificationAsync(userId, title, message, data);

        _logger.LogInformation("System alert sent to user {UserId}", userId);
    }

    public async Task SendBatteryLowNotificationAsync(int workerId, string workerName, int batteryLevel)
    {
        var title = "تنبيه بطارية منخفضة";
        var body = $"بطارية العامل {workerName} منخفضة ({batteryLevel}%). قد لا يتمكن من إكمال مهامه.";
        var data = new Dictionary<string, string>
        {
            { "workerId", workerId.ToString() },
            { "batteryLevel", batteryLevel.ToString() },
            { "type", "battery_low" }
        };

        // get all supervisors to notify them
        var supervisors = await _users.GetByRoleAsync(UserRole.Supervisor);

        foreach (var supervisor in supervisors)
        {
            var notification = new AppNotification
            {
                UserId = supervisor.UserId,
                MunicipalityId = supervisor.MunicipalityId,
                Title = title,
                Message = body,
                Type = NotificationType.BatteryLow,
                IsRead = false,
                IsSent = true,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { workerId, batteryLevel })
            };

            await _notifications.AddAsync(notification);

            // send push notification
            await SendPushNotificationAsync(supervisor.UserId, title, body, data);
        }

        await _notifications.SaveChangesAsync();
        _logger.LogInformation("Battery low notification sent for worker {WorkerId} ({BatteryLevel}%)", workerId, batteryLevel);
    }

    public async Task SendTaskAutoRejectedToWorkerAsync(int workerId, int taskId, string taskTitle, string reason, int distanceMeters)
    {
        var title = "⚠️ تم رفض إثبات المهمة";
        var body = $"تم رفض إثبات المهمة \"{taskTitle}\" تلقائياً.\n{reason}\nالمسافة من موقع المهمة: {distanceMeters} متر";
        var data = new Dictionary<string, string>
        {
            { "taskId", taskId.ToString() },
            { "type", "task_auto_rejected" },
            { "distanceMeters", distanceMeters.ToString() }
        };

        // get user to get their municipality
        var user = await _users.GetByIdAsync(workerId);

        var notification = new AppNotification
        {
            UserId = workerId,
            MunicipalityId = user?.MunicipalityId ?? 0,
            Title = title,
            Message = body,
            Type = NotificationType.TaskUpdated,
            IsRead = false,
            IsSent = true,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { taskId, reason, distanceMeters })
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        await SendPushNotificationAsync(workerId, title, body, data);
        _logger.LogInformation("Task auto-rejected notification sent to worker {WorkerId} for task {TaskId}", workerId, taskId);
    }

    public async Task SendTaskAutoRejectedToSupervisorsAsync(int taskId, string taskTitle, string workerName, string reason, int distanceMeters)
    {
        var title = "⚠️ رفض تلقائي لإثبات مهمة";
        var body = $"تم رفض إثبات العامل {workerName} للمهمة \"{taskTitle}\" تلقائياً.\n{reason}\nالمسافة: {distanceMeters} متر";
        var data = new Dictionary<string, string>
        {
            { "taskId", taskId.ToString() },
            { "type", "task_auto_rejected_supervisor" },
            { "distanceMeters", distanceMeters.ToString() }
        };

        var supervisors = await _users.GetByRoleAsync(UserRole.Supervisor);

        foreach (var supervisor in supervisors)
        {
            var notification = new AppNotification
            {
                UserId = supervisor.UserId,
                MunicipalityId = supervisor.MunicipalityId,
                Title = title,
                Message = body,
                Type = NotificationType.SystemAlert,
                IsRead = false,
                IsSent = true,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { taskId, workerName, reason, distanceMeters })
            };

            await _notifications.AddAsync(notification);
            await SendPushNotificationAsync(supervisor.UserId, title, body, data);
        }

        await _notifications.SaveChangesAsync();
        _logger.LogInformation("Task auto-rejected notification sent to supervisors for task {TaskId}", taskId);
    }

    public async Task SendWarningIssuedToWorkerAsync(int workerId, string reason, int totalWarnings)
    {
        var title = "⚠️ تحذير";
        var body = $"تم إصدار تحذير: {reason}\nإجمالي التحذيرات: {totalWarnings}";
        var data = new Dictionary<string, string>
        {
            { "type", "warning_issued" },
            { "totalWarnings", totalWarnings.ToString() }
        };

        // get user to get their municipality
        var user = await _users.GetByIdAsync(workerId);

        var notification = new AppNotification
        {
            UserId = workerId,
            MunicipalityId = user?.MunicipalityId ?? 0,
            Title = title,
            Message = body,
            Type = NotificationType.SystemAlert,
            IsRead = false,
            IsSent = true,
            CreatedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { reason, totalWarnings })
        };

        await _notifications.AddAsync(notification);
        await _notifications.SaveChangesAsync();

        await SendPushNotificationAsync(workerId, title, body, data);
        _logger.LogInformation("Warning notification sent to worker {WorkerId}, total warnings: {TotalWarnings}", workerId, totalWarnings);
    }

    public async Task SendWarningAlertToSupervisorsAsync(int workerId, string workerName, string reason, int totalWarnings)
    {
        var title = totalWarnings >= 3 ? "🚨 عامل وصل للحد الأقصى من التحذيرات" : "⚠️ تحذير لعامل";
        var body = $"تم إصدار تحذير للعامل {workerName}.\nالسبب: {reason}\nإجمالي التحذيرات: {totalWarnings}";
        var data = new Dictionary<string, string>
        {
            { "workerId", workerId.ToString() },
            { "type", "worker_warning_alert" },
            { "totalWarnings", totalWarnings.ToString() }
        };

        var supervisors = await _users.GetByRoleAsync(UserRole.Supervisor);

        foreach (var supervisor in supervisors)
        {
            var notification = new AppNotification
            {
                UserId = supervisor.UserId,
                MunicipalityId = supervisor.MunicipalityId,
                Title = title,
                Message = body,
                Type = NotificationType.SystemAlert,
                IsRead = false,
                IsSent = true,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { workerId, workerName, reason, totalWarnings })
            };

            await _notifications.AddAsync(notification);
            await SendPushNotificationAsync(supervisor.UserId, title, body, data);
        }

        await _notifications.SaveChangesAsync();
        _logger.LogInformation("Warning alert sent to supervisors for worker {WorkerId}, total warnings: {TotalWarnings}", workerId, totalWarnings);
    }

    // send push notif using firebase
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
            // token is bad so we remove it
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
