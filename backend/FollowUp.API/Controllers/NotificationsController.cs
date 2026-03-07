using AutoMapper;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Notifications;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Notifications")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationRepository _notices;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationRepository notices, IMapper mapper, ILogger<NotificationsController> logger)
    {
        _notices = notices;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "get all notifications for current user")]
    public async Task<IActionResult> GetMyNotifications()
    {
        // get user id from token
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // get all notifications
        var notifications = await _notices.GetUserNotificationsAsync(userId.Value);
        return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(
            notifications.Select(n => _mapper.Map<NotificationResponse>(n))));
    }

    [HttpGet("unread")]
    [SwaggerOperation(Summary = "get unread notifications only")]
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
    [SwaggerOperation(Summary = "get unread notifications count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var count = await _notices.GetUnreadCountAsync(userId.Value);

        return Ok(ApiResponse<int>.SuccessResponse(count));
    }

    [HttpPut("{id}/mark-read")]
    [SwaggerOperation(Summary = "mark one notification as read")]
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

    [HttpPut("mark-all-read")]
    [SwaggerOperation(Summary = "mark all notifications as read")]
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
    [SwaggerOperation(Summary = "delete a notification")]
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
