using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Notifications;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationRepository _notificationRepo;
    private readonly JawlahDbContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationRepository notificationRepo, JawlahDbContext context, ILogger<NotificationsController> logger)
    {
        _notificationRepo = notificationRepo;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notifications = await _notificationRepo.GetUserNotificationsAsync(userId.Value);

        return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(
            notifications.Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                IsSent = n.IsSent,
                CreatedAt = n.CreatedAt,
                SentAt = n.SentAt,
                ReadAt = n.ReadAt
            })));
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notifications = await _notificationRepo.GetUserNotificationsAsync(userId.Value, unreadOnly: true);

        return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(
            notifications.Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                IsSent = n.IsSent,
                CreatedAt = n.CreatedAt,
                SentAt = n.SentAt,
                ReadAt = n.ReadAt
            })));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notificationRepo.GetUnreadCountAsync(userId.Value);

        return Ok(ApiResponse<int>.SuccessResponse(count));
    }

    [HttpPut("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notification = await _notificationRepo.GetByIdAsync(id);
        if (notification == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Notification not found"));

        if (notification.UserId != userId.Value)
            return Forbid();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _notificationRepo.UpdateAsync(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", id, userId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Notification marked as read"));
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await _notificationRepo.MarkAllAsReadAsync(userId.Value);
        await _context.SaveChangesAsync();

        _logger.LogInformation("All notifications marked as read for user {UserId}", userId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "All notifications marked as read"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notification = await _notificationRepo.GetByIdAsync(id);
        if (notification == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Notification not found"));

        if (notification.UserId != userId.Value)
            return Forbid();

        await _notificationRepo.DeleteAsync(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification {NotificationId} deleted by user {UserId}", id, userId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Notification deleted successfully"));
    }
}
