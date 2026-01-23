using System.Security.Claims;
using AutoMapper;
using FollowUp.API;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Users;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using FollowUp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;
using AttendanceStatus = FollowUp.Core.Enums.AttendanceStatus;

namespace FollowUp.API.Controllers;

// this controller handle user managment
[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly FollowUpDbContext _context;
    private readonly IAuthService _auth;
    private readonly INotificationService _notifications;
    private readonly ILogger<UsersController> _logger;
    private readonly IMapper _mapper;

    // battery threshold for low battery warning
    private const int LowBatteryThreshold = 20;

    public UsersController(
        IUserRepository users,
        IZoneRepository zones,
        FollowUpDbContext context,
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

    // update user (admin only) - supports updating supervisorId for worker transfer
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // Update supervisorId if provided (for worker transfer)
        if (request.SupervisorId.HasValue)
        {
            if (user.Role != UserRole.Worker)
                return BadRequest(ApiResponse<object>.ErrorResponse("يمكن تعيين مشرف للعمال فقط"));

            if (request.SupervisorId.Value > 0)
            {
                var supervisor = await _users.GetByIdAsync(request.SupervisorId.Value);
                if (supervisor == null || supervisor.Role != UserRole.Supervisor)
                    return BadRequest(ApiResponse<object>.ErrorResponse("المشرف غير موجود أو غير صالح"));
            }

            user.SupervisorId = request.SupervisorId.Value > 0 ? request.SupervisorId.Value : null;
            _logger.LogInformation("Worker {WorkerId} transferred to supervisor {SupervisorId}", id, request.SupervisorId);
        }

        // Update other fields if provided
        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;

        if (request.Status.HasValue)
            user.Status = request.Status.Value;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_mapper.Map<UserResponse>(user)));
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

        // FIX 9: PREVENT SUPERVISOR DELETION IF HAS WORKERS
        // Referential Integrity - Supervisors cannot be deleted while managing workers
        if (user.Role == UserRole.Supervisor)
        {
            var supervisedWorkersCount = await _context.Users
                .Where(u => u.SupervisorId == id && u.Status != UserStatus.Inactive)
                .CountAsync();

            if (supervisedWorkersCount > 0)
            {
                _logger.LogWarning("Admin attempted to delete supervisor {SupervisorId} who has {WorkerCount} active workers",
                    id, supervisedWorkersCount);

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"لا يمكن حذف المشرف لأنه مسؤول عن {supervisedWorkersCount} عامل نشط. " +
                    "يرجى إعادة تعيين العمّال إلى مشرف آخر أولاً."
                ));
            }
        }

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

    // update device ID for a worker (admin can manually set device binding)
    [HttpPut("{id}/device-id")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> UpdateDeviceId(int id, [FromBody] UpdateDeviceIdRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // only workers need device binding
        if (user.Role != UserRole.Worker)
            return BadRequest(ApiResponse<object>.ErrorResponse("تحديث الجهاز متاح للعمال فقط"));

        var oldDeviceId = user.RegisteredDeviceId;
        user.RegisteredDeviceId = request.DeviceId;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Device ID updated for user {UserId}. Old: {OldDeviceId}, New: {NewDeviceId}",
            id, oldDeviceId ?? "none", request.DeviceId);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            message = "تم تحديث معرف الجهاز بنجاح"
        }));
    }

    // worker reports battery level - save to DB and notify supervisor if low
    [HttpPost("battery-status")]
    [Authorize(Roles = "Worker")]
    public async Task<IActionResult> ReportBattery([FromBody] BatteryReportRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // FIX 7: RATE LIMITING - Minimum 2 minutes between battery reports
        if (user.LastBatteryReportTime.HasValue)
        {
            var timeSinceLastReport = DateTime.UtcNow - user.LastBatteryReportTime.Value;
            if (timeSinceLastReport.TotalMinutes < 2)
            {
                _logger.LogDebug("Battery report from user {UserId} ignored - too frequent ({Seconds}s since last)",
                    userId, timeSinceLastReport.TotalSeconds);

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    received = false,
                    message = "تم تجاهل التقرير - تقارير متكررة"
                }));
            }
        }

        // FIX 8: UNREALISTIC BATTERY CHANGE DETECTION
        if (user.LastBatteryLevel.HasValue && user.LastBatteryReportTime.HasValue)
        {
            var batteryDelta = Math.Abs(request.BatteryLevel - user.LastBatteryLevel.Value);
            var timeDelta = DateTime.UtcNow - user.LastBatteryReportTime.Value;

            // Battery shouldn't change more than 30% in 5 minutes (unless charging/discharging rapidly)
            if (batteryDelta > 30 && timeDelta.TotalMinutes < 5 && !request.IsCharging)
            {
                _logger.LogWarning("Suspicious battery report from user {UserId}: {Old}% to {New}% in {Minutes:F1} minutes",
                    userId, user.LastBatteryLevel, request.BatteryLevel, timeDelta.TotalMinutes);

                // Still accept it but flag as suspicious (could be legitimate in some cases)
            }

            // Battery can't change more than 50% instantly (definitely wrong)
            if (batteryDelta > 50 && timeDelta.TotalMinutes < 1)
            {
                _logger.LogError("Invalid battery report from user {UserId}: {Old}% to {New}% in {Seconds}s - REJECTED",
                    userId, user.LastBatteryLevel, request.BatteryLevel, timeDelta.TotalSeconds);

                return BadRequest(ApiResponse<object>.ErrorResponse("تقرير البطارية غير صحيح - تغيير غير واقعي"));
            }
        }

        // update battery fields in user entity
        user.LastBatteryLevel = request.BatteryLevel;
        user.LastBatteryReportTime = request.Timestamp ?? DateTime.UtcNow;
        user.IsLowBattery = request.IsLowBattery || (request.BatteryLevel <= LowBatteryThreshold);

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        // check if battery is low and notify supervisors
        if (user.IsLowBattery && !request.IsCharging)
        {
            _logger.LogWarning("Worker {WorkerId} has low battery: {BatteryLevel}%", userId.Value, request.BatteryLevel);

            // send notification to supervisors
            await _notifications.SendBatteryLowNotificationAsync(userId.Value, user.FullName, request.BatteryLevel);
        }

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            received = true,
            message = "تم تحديث حالة البطارية"
        }));
    }

    // get workers assigned to current supervisor
    [HttpGet("my-workers")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> GetMyWorkers()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // get all workers where SupervisorId matches current user
        var workers = await _context.Users
            .Where(u => u.SupervisorId == userId.Value && u.Role == UserRole.Worker)
            .Include(u => u.AssignedTasks.Where(t => t.Status == TaskStatus.InProgress || t.Status == TaskStatus.Pending))
            .Include(u => u.AttendanceRecords.Where(a => a.CheckInEventTime.Date == DateTime.UtcNow.Date))
            .ToListAsync();

        // calculate stats for each worker
        var workerResponses = workers.Select(worker =>
        {
            var todayAttendance = worker.AttendanceRecords.FirstOrDefault();
            var isActive = todayAttendance != null && todayAttendance.Status == AttendanceStatus.CheckedIn;

            var activeTasksCount = worker.AssignedTasks.Count(t => t.Status == TaskStatus.InProgress || t.Status == TaskStatus.Pending);

            // get completed tasks this week
            var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            var completedThisWeek = _context.Tasks
                .Where(t => t.AssignedToUserId == worker.UserId &&
                           t.Status == TaskStatus.Completed &&
                           t.CompletedAt >= weekStart)
                .Count();

            // calculate completion rate (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentTasks = _context.Tasks
                .Where(t => t.AssignedToUserId == worker.UserId && t.CreatedAt >= thirtyDaysAgo)
                .ToList();

            var completionRate = recentTasks.Any()
                ? (double)recentTasks.Count(t => t.Status == TaskStatus.Completed) / recentTasks.Count * 100
                : 0;

            return new
            {
                userId = worker.UserId,
                fullName = worker.FullName,
                phoneNumber = worker.PhoneNumber,
                department = worker.Department,
                workerType = worker.WorkerType?.ToString(),
                isActive,
                lastCheckIn = todayAttendance?.CheckInEventTime,
                activeTasksCount,
                completedTasksThisWeek = completedThisWeek,
                completionRate = Math.Round(completionRate, 1),
                warningCount = worker.WarningCount,
                lastBatteryLevel = worker.LastBatteryLevel,
                isLowBattery = worker.IsLowBattery
            };
        }).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(workerResponses));
    }

    // get detailed user profile (for supervisor viewing worker profile)
    [HttpGet("{id}/profile")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUserProfile(int id)
    {
        var user = await _context.Users
            .Include(u => u.AssignedZones.Where(uz => uz.IsActive))
                .ThenInclude(uz => uz.Zone)
            .Include(u => u.AssignedTasks.OrderByDescending(t => t.CreatedAt).Take(10))
            .Include(u => u.AttendanceRecords.OrderByDescending(a => a.CheckInEventTime).Take(7))
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // verify supervisor can only access their own workers
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole == "Supervisor" && user.SupervisorId != currentUserId)
            return Forbid();

        // calculate performance stats
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentTasks = await _context.Tasks
            .Where(t => t.AssignedToUserId == id && t.CreatedAt >= thirtyDaysAgo)
            .ToListAsync();

        var completionRate = recentTasks.Any()
            ? (double)recentTasks.Count(t => t.Status == TaskStatus.Completed) / recentTasks.Count * 100
            : 0;

        var avgCompletionTime = recentTasks
            .Where(t => t.Status == TaskStatus.Completed && t.CompletedAt.HasValue)
            .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours)
            .DefaultIfEmpty(0)
            .Average();

        var profile = new
        {
            userId = user.UserId,
            fullName = user.FullName,
            username = user.Username,
            email = user.Email,
            phoneNumber = user.PhoneNumber,
            role = user.Role.ToString(),
            workerType = user.WorkerType?.ToString(),
            department = user.Department,
            status = user.Status.ToString(),
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt,
            supervisorId = user.SupervisorId,

            // work schedule
            expectedStartTime = user.ExpectedStartTime.ToString(@"hh\:mm"),
            expectedEndTime = user.ExpectedEndTime.ToString(@"hh\:mm"),
            graceMinutes = user.GraceMinutes,

            // warnings and battery
            warningCount = user.WarningCount,
            lastWarningAt = user.LastWarningAt,
            lastWarningReason = user.LastWarningReason,
            lastBatteryLevel = user.LastBatteryLevel,
            lastBatteryReportTime = user.LastBatteryReportTime,
            isLowBattery = user.IsLowBattery,

            // assigned zones
            assignedZones = user.AssignedZones.Select(uz => new
            {
                zoneId = uz.ZoneId,
                zoneName = uz.Zone.ZoneName,
                assignedAt = uz.AssignedAt
            }).ToList(),

            // performance stats
            completionRate = Math.Round(completionRate, 1),
            avgCompletionTimeHours = Math.Round(avgCompletionTime, 1),
            totalTasksLast30Days = recentTasks.Count,
            completedTasksLast30Days = recentTasks.Count(t => t.Status == TaskStatus.Completed),

            // recent tasks (last 10)
            recentTasks = user.AssignedTasks.Select(t => new
            {
                taskId = t.TaskId,
                title = t.Title,
                status = t.Status.ToString(),
                priority = t.Priority.ToString(),
                createdAt = t.CreatedAt,
                dueDate = t.DueDate,
                completedAt = t.CompletedAt,
                progressPercentage = t.ProgressPercentage
            }).ToList(),

            // recent attendance (last 7 days)
            recentAttendance = user.AttendanceRecords.Select(a => new
            {
                attendanceId = a.AttendanceId,
                checkInTime = a.CheckInEventTime,
                checkOutTime = a.CheckOutEventTime,
                status = a.Status.ToString(),
                lateMinutes = a.LateMinutes,
                isValidated = a.IsValidated,
                validationMessage = a.ValidationMessage
            }).ToList()
        };

        return Ok(ApiResponse<object>.SuccessResponse(profile));
    }

    // FIX 9 ENHANCEMENT: Transfer workers from one supervisor to another
    // This helps admins reassign workers before deleting/deactivating a supervisor
    [HttpPost("supervisors/{oldSupervisorId}/transfer-workers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TransferWorkers(
        int oldSupervisorId,
        [FromBody] TransferWorkersRequest request)
    {
        // Validate old supervisor exists
        var oldSupervisor = await _users.GetByIdAsync(oldSupervisorId);
        if (oldSupervisor == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المشرف القديم غير موجود"));

        if (oldSupervisor.Role != UserRole.Supervisor)
            return BadRequest(ApiResponse<object>.ErrorResponse("المستخدم المحدد ليس مشرفاً"));

        // Validate new supervisor exists and is active
        var newSupervisor = await _users.GetByIdAsync(request.NewSupervisorId);
        if (newSupervisor == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المشرف الجديد غير موجود"));

        if (newSupervisor.Role != UserRole.Supervisor)
            return BadRequest(ApiResponse<object>.ErrorResponse("المستخدم الجديد يجب أن يكون مشرفاً"));

        if (newSupervisor.Status != UserStatus.Active)
            return BadRequest(ApiResponse<object>.ErrorResponse("المشرف الجديد يجب أن يكون نشطاً"));

        // Can't transfer to same supervisor
        if (oldSupervisorId == request.NewSupervisorId)
            return BadRequest(ApiResponse<object>.ErrorResponse("لا يمكن نقل العمّال إلى نفس المشرف"));

        // Get all active workers under old supervisor
        var workers = await _context.Users
            .Where(u => u.SupervisorId == oldSupervisorId &&
                       u.Role == UserRole.Worker &&
                       u.Status != UserStatus.Inactive)
            .ToListAsync();

        if (!workers.Any())
        {
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                transferredWorkers = 0,
                message = "لا يوجد عمّال نشطون لنقلهم"
            }));
        }

        // Transfer all workers to new supervisor
        foreach (var worker in workers)
        {
            worker.SupervisorId = request.NewSupervisorId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Admin transferred {WorkerCount} workers from supervisor {OldSupervisorId} to supervisor {NewSupervisorId}",
            workers.Count, oldSupervisorId, request.NewSupervisorId);

        // TODO: Notify new supervisor about new workers
        // Enhancement: Add SendWorkerTransferNotificationAsync to INotificationService
        // await _notifications.SendWorkerTransferNotificationAsync(...)
        _logger.LogInformation(
            "Workers transferred successfully. New supervisor {NewSupervisorName} should be notified about {WorkerCount} new workers",
            newSupervisor.FullName, workers.Count);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            transferredWorkers = workers.Count,
            message = $"تم نقل {workers.Count} عامل بنجاح من {oldSupervisor.FullName} إلى {newSupervisor.FullName}",
            workerNames = workers.Select(w => w.FullName).ToList()
        }));
    }
}
