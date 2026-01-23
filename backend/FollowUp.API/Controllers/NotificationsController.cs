using AutoMapper;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Notifications;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FollowUp.API.Controllers;

// this controller handle user notifications
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

    // get all notifications for current user
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        // get user id from token
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // get all notifications
        var notifications = await _notices.GetUserNotificationsAsync(userId.Value);

        // return as list
        return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(
            notifications.Select(n => _mapper.Map<NotificationResponse>(n))));
    }

    // get only unread notifications
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

    // get count of unread notifications
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notices.GetUnreadCountAsync(userId.Value);

        return Ok(ApiResponse<int>.SuccessResponse(count));
    }

    // mark single notification as read
    [HttpPut("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notification = await _notices.GetByIdAsync(id);
        if (notification == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الإشعار غير موجود"));

        // check if notification belong to user
        if (notification.UserId != userId.Value)
            return Forbid();

        await _notices.MarkAsReadAsync(id);
        await _notices.SaveChangesAsync();

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تحديد الإشعار كمقروء"));
    }

    // mark all notifications as read
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

    // delete single notification
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var notification = await _notices.GetByIdAsync(id);
        if (notification == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الإشعار غير موجود"));

        // check if notification belong to user
        if (notification.UserId != userId.Value)
            return Forbid();

        await _notices.DeleteAsync(notification);
        await _notices.SaveChangesAsync();

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم حذف الإشعار بنجاح"));
    }
}
