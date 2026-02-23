using AutoMapper;
using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Sync;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FollowUp.API.Controllers;

// this controller handle offline sync from mobile app
[Route("api/[controller]")]
[Authorize]
public class SyncController : BaseApiController
{
    private readonly ITaskRepository _tasks;
    private readonly IAttendanceRepository _attendance;
    private readonly IIssueRepository _issues;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ITaskRepository tasks,
        IAttendanceRepository attendance,
        IIssueRepository issues,
        IUserRepository users,
        IMapper mapper,
        ILogger<SyncController> logger)
    {
        _tasks = tasks;
        _attendance = attendance;
        _issues = issues;
        _users = users;
        _mapper = mapper;
        _logger = logger;
    }

    // sync attendance records from mobile
    [HttpPost("attendance/batch")]
    public async Task<IActionResult> SyncAttendanceBatch([FromBody] BatchSyncRequest<AttendanceSyncDto> request)
    {
        // get current user
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        // check request has items
        if (request?.Items == null || !request.Items.Any())
            return BadRequest(ApiResponse<BatchSyncResponse>.ErrorResponse("لا توجد عناصر للمزامنة"));

        // get user to get their municipality
        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null) return Unauthorized();

        var results = new List<SyncResult>();

        // process each attendance item
        foreach (var item in request.Items)
        {
            try
            {
                // EventTime validation: reject timestamps too far in the past or future
                var now = DateTime.UtcNow;
                if (item.CheckInEventTime > now.AddMinutes(5))
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "وقت تسجيل الحضور في المستقبل" });
                    continue;
                }
                if (item.CheckInEventTime < now.AddDays(-7))
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "وقت تسجيل الحضور قديم جداً (أكثر من 7 أيام)" });
                    continue;
                }
                if (item.CheckOutEventTime.HasValue && item.CheckOutEventTime < item.CheckInEventTime)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "وقت الانصراف قبل وقت الحضور" });
                    continue;
                }

                // check if already exist for today
                var existing = await _attendance.GetTodayAttendanceAsync(userId.Value);
                if (existing != null)
                {
                    // SyncVersion conflict check for attendance
                    if (item.SyncVersion > 0 && existing.SyncVersion > item.SyncVersion)
                    {
                        results.Add(new SyncResult { ClientId = item.ClientId, ServerId = existing.AttendanceId, Success = false, Message = "تعارض: نسخة الحضور على الخادم أحدث", ConflictResolution = "ServerWins", ServerVersion = existing.SyncVersion });
                        continue;
                    }

                    // update existing record
                    existing.CheckOutEventTime = item.CheckOutEventTime;
                    existing.CheckOutLatitude = item.CheckOutLatitude;
                    existing.CheckOutLongitude = item.CheckOutLongitude;
                    existing.CheckOutAccuracyMeters = item.CheckOutAccuracyMeters;
                    existing.Status = item.CheckOutEventTime.HasValue ? Core.Enums.AttendanceStatus.CheckedOut : Core.Enums.AttendanceStatus.CheckedIn;
                    existing.SyncVersion++;
                    existing.CheckOutSyncTime = DateTime.UtcNow;

                    // Calculate work duration and overtime/early-leave from EventTime
                    if (item.CheckOutEventTime.HasValue)
                    {
                        var duration = item.CheckOutEventTime.Value - existing.CheckInEventTime;
                        if (duration < TimeSpan.Zero) duration = duration.Duration();
                        if (duration > TimeSpan.FromHours(23)) duration = TimeSpan.FromHours(23);
                        existing.WorkDuration = duration;

                        var expectedEnd = item.CheckOutEventTime.Value.Date.Add(user.ExpectedEndTime);
                        if (item.CheckOutEventTime.Value > expectedEnd)
                        {
                            existing.OvertimeMinutes = (int)(item.CheckOutEventTime.Value - expectedEnd).TotalMinutes;
                            if (existing.AttendanceType != "Late") existing.AttendanceType = "Overtime";
                        }
                        else if (item.CheckOutEventTime.Value < expectedEnd.AddMinutes(-30))
                        {
                            existing.EarlyLeaveMinutes = (int)(expectedEnd - item.CheckOutEventTime.Value).TotalMinutes;
                            if (existing.AttendanceType != "Late") existing.AttendanceType = "EarlyLeave";
                        }
                    }

                    await _attendance.UpdateAsync(existing);
                    await _attendance.SaveChangesAsync();
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = existing.AttendanceId, Success = true, ServerVersion = existing.SyncVersion });
                }
                else
                {
                    // Calculate late arrival from EventTime (not server time)
                    var expectedStart = item.CheckInEventTime.Date.Add(user.ExpectedStartTime);
                    var graceEnd = expectedStart.AddMinutes(user.GraceMinutes);
                    int lateMinutes = 0;
                    string attendanceType = "OnTime";

                    if (item.CheckInEventTime > graceEnd)
                    {
                        lateMinutes = (int)(item.CheckInEventTime - graceEnd).TotalMinutes;
                        attendanceType = "Late";
                    }

                    // create new record with user's municipality
                    var attendance = new Attendance
                    {
                        UserId = userId.Value,
                        MunicipalityId = user.MunicipalityId,
                        CheckInEventTime = item.CheckInEventTime,
                        CheckOutEventTime = item.CheckOutEventTime,
                        CheckInLatitude = item.CheckInLatitude,
                        CheckInLongitude = item.CheckInLongitude,
                        CheckInAccuracyMeters = item.CheckInAccuracyMeters,
                        CheckOutLatitude = item.CheckOutLatitude,
                        CheckOutLongitude = item.CheckOutLongitude,
                        CheckOutAccuracyMeters = item.CheckOutAccuracyMeters,
                        IsValidated = item.IsValidated,
                        ValidationMessage = InputSanitizer.SanitizeString(item.ValidationMessage, 500),
                        Status = item.CheckOutEventTime.HasValue ? Core.Enums.AttendanceStatus.CheckedOut : Core.Enums.AttendanceStatus.CheckedIn,
                        LateMinutes = lateMinutes,
                        AttendanceType = attendanceType,
                        IsSynced = true,
                        SyncVersion = 1,
                        CheckInSyncTime = DateTime.UtcNow,
                        CheckOutSyncTime = item.CheckOutEventTime.HasValue ? DateTime.UtcNow : null
                    };

                    // Calculate checkout metrics if checkout time exists
                    if (item.CheckOutEventTime.HasValue)
                    {
                        var duration = item.CheckOutEventTime.Value - item.CheckInEventTime;
                        if (duration < TimeSpan.Zero) duration = duration.Duration();
                        if (duration > TimeSpan.FromHours(23)) duration = TimeSpan.FromHours(23);
                        attendance.WorkDuration = duration;

                        var expectedEnd = item.CheckOutEventTime.Value.Date.Add(user.ExpectedEndTime);
                        if (item.CheckOutEventTime.Value > expectedEnd)
                        {
                            attendance.OvertimeMinutes = (int)(item.CheckOutEventTime.Value - expectedEnd).TotalMinutes;
                            if (attendance.AttendanceType != "Late") attendance.AttendanceType = "Overtime";
                        }
                        else if (item.CheckOutEventTime.Value < expectedEnd.AddMinutes(-30))
                        {
                            attendance.EarlyLeaveMinutes = (int)(expectedEnd - item.CheckOutEventTime.Value).TotalMinutes;
                            if (attendance.AttendanceType != "Late") attendance.AttendanceType = "EarlyLeave";
                        }
                    }

                    await _attendance.AddAsync(attendance);
                    await _attendance.SaveChangesAsync();
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = attendance.AttendanceId, Success = true, ServerVersion = attendance.SyncVersion });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing attendance item for client {ClientId}", item.ClientId);
                results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "فشل المزامنة" });
            }
        }

        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse { Results = results }));
    }

    // sync task updates from mobile
    [HttpPost("tasks/batch")]
    public async Task<IActionResult> SyncTasksBatch([FromBody] BatchSyncRequest<TaskSyncDto> request)
    {
        // get current user
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        // check request has items
        if (request?.Items == null || !request.Items.Any())
            return BadRequest(ApiResponse<BatchSyncResponse>.ErrorResponse("لا توجد عناصر للمزامنة"));

        var results = new List<SyncResult>();

        // process each task update
        foreach (var item in request.Items)
        {
            try
            {
                // find task
                var task = await _tasks.GetByIdAsync(item.TaskId);
                if (task == null)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "المهمة غير موجودة" });
                    continue;
                }

                // check user can access this task (individual OR team task)
                bool canAccess = task.AssignedToUserId == userId.Value;
                if (!canAccess && task.IsTeamTask && task.TeamId.HasValue)
                {
                    var user = await _users.GetByIdAsync(userId.Value);
                    canAccess = user?.TeamId.HasValue == true && user.TeamId == task.TeamId;
                }
                if (!canAccess)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "غير مصرح" });
                    continue;
                }

                // SyncVersion conflict check: reject if server has newer data
                if (item.SyncVersion > 0 && task.SyncVersion > item.SyncVersion)
                {
                    results.Add(new SyncResult
                    {
                        ClientId = item.ClientId,
                        ServerId = task.TaskId,
                        Success = false,
                        Message = "تعارض: النسخة على الخادم أحدث",
                        ConflictResolution = "ServerWins",
                        ServerVersion = task.SyncVersion
                    });
                    continue;
                }

                // EventTime validation: reject future completion dates
                if (item.CompletedAt.HasValue && item.CompletedAt > DateTime.UtcNow.AddMinutes(5))
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = task.TaskId, Success = false, Message = "تاريخ الإكمال في المستقبل" });
                    continue;
                }

                // update task data
                task.Status = item.Status;
                task.CompletionNotes = InputSanitizer.SanitizeString(item.CompletionNotes, 1000);
                task.CompletedAt = item.CompletedAt;
                task.Latitude = item.Latitude;
                task.Longitude = item.Longitude;
                task.SyncTime = DateTime.UtcNow;
                task.SyncVersion++;

                await _tasks.UpdateAsync(task);
                results.Add(new SyncResult { ClientId = item.ClientId, ServerId = task.TaskId, Success = true, ServerVersion = task.SyncVersion });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing task item for client {ClientId}", item.ClientId);
                results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "فشل المزامنة" });
            }
        }

        await _tasks.SaveChangesAsync();
        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse { Results = results }));
    }

    // sync issues from mobile
    [HttpPost("issues/batch")]
    public async Task<IActionResult> SyncIssuesBatch([FromBody] BatchSyncRequest<IssueSyncDto> request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        if (request?.Items == null || !request.Items.Any())
            return BadRequest(ApiResponse<BatchSyncResponse>.ErrorResponse("لا توجد عناصر للمزامنة"));

        // get user to get their municipality
        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null) return Unauthorized();

        var results = new List<SyncResult>();

        foreach (var item in request.Items)
        {
            try
            {
                // Require ClientId for idempotent sync (mobile always generates UUID v4)
                if (string.IsNullOrEmpty(item.ClientId))
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "معرّف العنصر (ClientId) مطلوب للمزامنة" });
                    continue;
                }

                // Idempotent duplicate detection: check current batch + DB
                // Check current batch first (same request)
                var batchDuplicate = results.FirstOrDefault(r => r.ClientId == item.ClientId && r.Success);
                if (batchDuplicate != null)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = batchDuplicate.ServerId, Success = true, ServerVersion = 1 });
                    continue;
                }

                // Check database (cross-request deduplication: userId + clientId)
                var existingIssue = await _issues.GetByClientIdAsync(userId.Value, item.ClientId);
                if (existingIssue != null)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = existingIssue.IssueId, Success = true, ServerVersion = existingIssue.SyncVersion });
                    continue;
                }

                // EventTime validation: reject future or very old reports
                var syncNow = DateTime.UtcNow;
                if (item.ReportedAt > syncNow.AddMinutes(5))
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "تاريخ البلاغ في المستقبل" });
                    continue;
                }
                if (item.ReportedAt < syncNow.AddDays(-7))
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "تاريخ البلاغ قديم جداً (أكثر من 7 أيام)" });
                    continue;
                }

                // create new issue from offline data with user's municipality
                var issue = new Issue
                {
                    MunicipalityId = user.MunicipalityId,
                    ClientId = item.ClientId,
                    Title = InputSanitizer.SanitizeString(item.Title, 200),
                    Description = InputSanitizer.SanitizeString(item.Description, 2000),
                    Type = item.Type,
                    Severity = item.Severity,
                    Status = Core.Enums.IssueStatus.Reported,
                    ReportedByUserId = userId.Value,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    LocationDescription = InputSanitizer.SanitizeString(item.LocationDescription, 500),
                    PhotoUrl = item.PhotoUrl,
                    ReportedAt = item.ReportedAt,
                    EventTime = item.ReportedAt,
                    SyncTime = DateTime.UtcNow,
                    IsSynced = true,
                    SyncVersion = 1
                };

                await _issues.AddAsync(issue);
                await _issues.SaveChangesAsync();

                results.Add(new SyncResult
                {
                    ClientId = item.ClientId,
                    ServerId = issue.IssueId,
                    Success = true,
                    ServerVersion = issue.SyncVersion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing issue item for client {ClientId}", item.ClientId);
                results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "فشل المزامنة" });
            }
        }

        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse { Results = results }));
    }

    // get changes since last sync
    [HttpGet("changes")]
    public async Task<IActionResult> GetChanges([FromQuery] DateTime lastSyncTime)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        // get only changes after last sync time
        var tasks = await _tasks.GetTasksModifiedAfterAsync(userId.Value, lastSyncTime);
        var issues = await _issues.GetIssuesModifiedAfterAsync(userId.Value, lastSyncTime);

        var taskDtos = _mapper.Map<List<TaskSyncDto>>(tasks);
        var issueDtos = _mapper.Map<List<IssueSyncDto>>(issues);

        return Ok(ApiResponse<SyncChangesResponse>.SuccessResponse(new SyncChangesResponse
        {
            Tasks = taskDtos,
            Issues = issueDtos,
            LastSyncTime = lastSyncTime,
            CurrentServerTime = DateTime.UtcNow
        }));
    }
}
