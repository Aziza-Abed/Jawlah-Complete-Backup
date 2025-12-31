using AutoMapper;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Notifications;
using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationRepository _notices;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IMapper _mapper;

    public NotificationsController(INotificationRepository notices, ILogger<NotificationsController> logger, IMapper mapper)
    {
        _notices = notices;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        // 1. get the current user ID
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // 2. load all notifications for this user
        var notifications = await _notices.GetUserNotificationsAsync(userId.Value);

        // 3. return them as a list
        return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(
            notifications.Select(n => _mapper.Map<NotificationResponse>(n))));
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notifications = await _notices.GetUserNotificationsAsync(userId.Value, unreadOnly: true);

        return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(
            notifications.Select(n => _mapper.Map<NotificationResponse>(n))));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notices.GetUnreadCountAsync(userId.Value);

        return Ok(ApiResponse<int>.SuccessResponse(count));
    }

    [HttpPut("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notification = await _notices.GetByIdAsync(id);
        if (notification == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الإشعار غير موجود"));

        if (notification.UserId != userId.Value)
            return Forbid();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _notices.UpdateAsync(notification);
        await _notices.SaveChangesAsync();

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تحديد الإشعار كمقروء"));
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await _notices.MarkAllAsReadAsync(userId.Value);
        await _notices.SaveChangesAsync();

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تحديد جميع الإشعارات كمقروءة"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notification = await _notices.GetByIdAsync(id);
        if (notification == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الإشعار غير موجود"));

        if (notification.UserId != userId.Value)
            return Forbid();

        await _notices.DeleteAsync(notification);
        await _notices.SaveChangesAsync();

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم حذف الإشعار بنجاح"));
    }
}
