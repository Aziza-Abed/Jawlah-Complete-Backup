using System.Security.Claims;
using AutoMapper;
using Jawlah.API;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Users;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.API.Controllers;

// this controller handle user managment
[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly JawlahDbContext _context;
    private readonly IAuthService _auth;
    private readonly INotificationService _notifications;
    private readonly ILogger<UsersController> _logger;
    private readonly IMapper _mapper;

    // battery threshold for low battery warning
    private const int LowBatteryThreshold = 20;

    public UsersController(
        IUserRepository users,
        IZoneRepository zones,
        JawlahDbContext context,
        IAuthService auth,
        INotificationService notifications,
        ILogger<UsersController> logger,
        IMapper mapper)
    {
        _users = users;
        _zones = zones;
        _context = context;
        _auth = auth;
        _notifications = notifications;
        _logger = logger;
        _mapper = mapper;
    }

    // get all users with pagination
    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] UserStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // fix pagination
        if (pageSize < 1 || pageSize > 100) pageSize = 50;
        if (page < 1) page = 1;

        // get users from db
        var users = await _users.GetAllAsync();

        // filter by status if needed
        if (status.HasValue)
        {
            users = users.Where(u => u.Status == status.Value);
        }

        // paginate results
        var totalCount = users.Count();
        var pagedUsers = users
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            items = pagedUsers.Select(u => _mapper.Map<UserResponse>(u)),
            totalCount,
            page,
            pageSize
        }));
    }

    // get single user by id
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_mapper.Map<UserResponse>(user)));
    }

    // get users by role
    [HttpGet("by-role/{role}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUsersByRole(UserRole role)
    {
        var users = await _users.GetByRoleAsync(role);

        return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(
            users.Select(u => _mapper.Map<UserResponse>(u))));
    }

    // update user profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        // get current user
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // update fields
        // workers dont have email
        if (user.Role != UserRole.Worker && !string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
        }
        user.PhoneNumber = request.PhoneNumber;
        user.FullName = request.FullName;

        // save to db
        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated their profile", userId.Value);
        return Ok(ApiResponse<UserResponse>.SuccessResponse(_mapper.Map<UserResponse>(user)));
    }

    // change password
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // verify old password
        if (!_auth.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("رقم التعريف أو كلمة المرور غير صحيحة"));
        }

        // check new password is strong
        if (!Utils.InputSanitizer.IsStrongPassword(request.NewPassword))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص"));
        }

        // hash and save new password
        user.PasswordHash = _auth.HashPassword(request.NewPassword);

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("User {UserId} changed password", userId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تغيير كلمة المرور بنجاح"));
    }

    // update user status admin only
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        user.Status = request.Status;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("User {UserId} status updated to {Status}", id, request.Status);

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_mapper.Map<UserResponse>(user)));
    }

    // get count of active workers
    [HttpGet("active-workers-count")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetActiveWorkersCount()
    {
        var workers = await _users.GetByRoleAsync(UserRole.Worker);
        var activeCount = workers.Count(w => w.Status == UserStatus.Active);

        return Ok(ApiResponse<int>.SuccessResponse(activeCount));
    }

    // soft delete user
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // we dont really delete just set inactive
        user.Status = UserStatus.Inactive;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("User {UserId} soft deleted by admin", id);

        return NoContent();
    }

    // SR1.6: Admin can reset user password
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetUserPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // check new password is strong
        if (!Utils.InputSanitizer.IsStrongPassword(request.NewPassword))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص"));
        }

        // hash and save new password
        user.PasswordHash = _auth.HashPassword(request.NewPassword);

        // reset lockout if any
        user.FailedLoginAttempts = 0;
        user.LockoutEndTime = null;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Admin reset password for user {UserId}", id);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم إعادة تعيين كلمة المرور بنجاح"));
    }

    // zone assignment endpoints

    // get zones assigned to user
    [HttpGet("{id}/zones")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUserZones(int id)
    {
        var user = await _users.GetUserWithZonesAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // get active zones only
        var zones = user.AssignedZones
            .Where(uz => uz.IsActive)
            .Select(uz => new
            {
                uz.ZoneId,
                uz.Zone.ZoneName,
                uz.Zone.ZoneCode,
                uz.AssignedAt
            });

        return Ok(ApiResponse<object>.SuccessResponse(zones));
    }

    // assign zones to worker
    [HttpPost("{id}/zones")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> AssignZones(int id, [FromBody] AssignZonesRequest request)
    {
        var user = await _users.GetUserWithZonesAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // only workers can have zones
        if (user.Role != UserRole.Worker)
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن تعيين المناطق للعمال فقط"));

        // need at least one zone
        if (request.ZoneIds == null || !request.ZoneIds.Any())
            return BadRequest(ApiResponse<object>.ErrorResponse("يجب تحديد منطقة واحدة على الأقل"));

        // check all zones exist
        var zones = await _zones.GetZonesByIdsAsync(request.ZoneIds);
        var foundZoneIds = zones.Select(z => z.ZoneId).ToHashSet();
        var invalidZoneIds = request.ZoneIds.Where(zid => !foundZoneIds.Contains(zid)).ToList();

        if (invalidZoneIds.Any())
            return BadRequest(ApiResponse<object>.ErrorResponse($"المناطق غير موجودة: {string.Join(", ", invalidZoneIds)}"));

        var currentUserId = GetCurrentUserId() ?? 0;

        // deactivate old zones
        foreach (var uz in user.AssignedZones.Where(uz => uz.IsActive))
        {
            uz.IsActive = false;
        }

        // add new zone assignments
        foreach (var zoneId in request.ZoneIds)
        {
            var existing = user.AssignedZones.FirstOrDefault(uz => uz.ZoneId == zoneId);
            if (existing != null)
            {
                // reactivate existing
                existing.IsActive = true;
                existing.AssignedAt = DateTime.UtcNow;
                existing.AssignedByUserId = currentUserId;
            }
            else
            {
                // create new assignment
                user.AssignedZones.Add(new UserZone
                {
                    UserId = id,
                    ZoneId = zoneId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedByUserId = currentUserId,
                    IsActive = true
                });
            }
        }

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Zones assigned to user {UserId}: {ZoneIds}", id, string.Join(", ", request.ZoneIds));

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            message = "تم تعيين المناطق بنجاح",
            assignedZones = request.ZoneIds.Count()
        }));
    }

    // remove zone from worker
    [HttpDelete("{id}/zones/{zoneId}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> RemoveZoneAssignment(int id, int zoneId)
    {
        var user = await _users.GetUserWithZonesAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        var assignment = user.AssignedZones.FirstOrDefault(uz => uz.ZoneId == zoneId && uz.IsActive);
        if (assignment == null)
            return NotFound(ApiResponse<object>.ErrorResponse("لم يتم تعيين هذه المنطقة للمستخدم"));

        // cant remove last zone
        var activeZonesCount = user.AssignedZones.Count(uz => uz.IsActive);
        if (activeZonesCount <= 1)
            return BadRequest(ApiResponse<object>.ErrorResponse("لا يمكن إزالة المنطقة الأخيرة. يجب أن يكون للعامل منطقة واحدة على الأقل"));

        // deactivate instead of delete
        assignment.IsActive = false;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Zone {ZoneId} removed from user {UserId}", zoneId, id);

        return NoContent();
    }

    // reset device binding for a worker (when they get a new phone)
    [HttpPost("{id}/reset-device")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ResetDeviceBinding(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // only workers need device binding
        if (user.Role != UserRole.Worker)
            return BadRequest(ApiResponse<object>.ErrorResponse("إعادة تعيين الجهاز متاحة للعمال فقط"));

        var oldDeviceId = user.RegisteredDeviceId;
        user.RegisteredDeviceId = null;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Device binding reset for user {UserId}. Old device: {OldDeviceId}", id, oldDeviceId ?? "none");

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            message = "تم إعادة تعيين ربط الجهاز بنجاح. يمكن للعامل تسجيل الدخول من جهاز جديد."
        }));
    }

    // worker reports battery level - notify supervisor if low
    [HttpPost("battery")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> ReportBattery([FromBody] BatteryReportRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // check if battery is low and notify supervisors
        if (request.BatteryLevel <= LowBatteryThreshold)
        {
            _logger.LogWarning("Worker {WorkerId} has low battery: {BatteryLevel}%", userId.Value, request.BatteryLevel);

            // send notification to supervisors
            await _notifications.SendBatteryLowNotificationAsync(userId.Value, user.FullName, request.BatteryLevel);
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { received = true }));
    }
}
