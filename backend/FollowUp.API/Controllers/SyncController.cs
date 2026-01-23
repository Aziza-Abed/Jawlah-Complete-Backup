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
                // check if already exist for today
                var existing = await _attendance.GetTodayAttendanceAsync(userId.Value);
                if (existing != null)
                {
                    // update existing record
                    existing.CheckOutEventTime = item.CheckOutEventTime;
                    existing.CheckOutLatitude = item.CheckOutLatitude;
                    existing.CheckOutLongitude = item.CheckOutLongitude;
                    existing.Status = item.CheckOutEventTime.HasValue ? Core.Enums.AttendanceStatus.CheckedOut : Core.Enums.AttendanceStatus.CheckedIn;
                    existing.SyncVersion++;
                    existing.CheckOutSyncTime = DateTime.UtcNow;

                    await _attendance.UpdateAsync(existing);
                    await _attendance.SaveChangesAsync();
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = existing.AttendanceId, Success = true });
                }
                else
                {
                    // create new record with user's municipality
                    var attendance = new Attendance
                    {
                        UserId = userId.Value,
                        MunicipalityId = user.MunicipalityId,
                        CheckInEventTime = item.CheckInEventTime,
                        CheckOutEventTime = item.CheckOutEventTime,
                        CheckInLatitude = item.CheckInLatitude,
                        CheckInLongitude = item.CheckInLongitude,
                        CheckOutLatitude = item.CheckOutLatitude,
                        CheckOutLongitude = item.CheckOutLongitude,
                        IsValidated = item.IsValidated,
                        ValidationMessage = item.ValidationMessage,
                        Status = item.CheckOutEventTime.HasValue ? Core.Enums.AttendanceStatus.CheckedOut : Core.Enums.AttendanceStatus.CheckedIn,
                        IsSynced = true,
                        SyncVersion = 1,
                        CheckInSyncTime = DateTime.UtcNow,
                        CheckOutSyncTime = item.CheckOutEventTime.HasValue ? DateTime.UtcNow : null
                    };

                    await _attendance.AddAsync(attendance);
                    await _attendance.SaveChangesAsync();
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = attendance.AttendanceId, Success = true });
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

                // check user owns this task
                if (task.AssignedToUserId != userId.Value)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "غير مصرح" });
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
                // create new issue from offline data with user's municipality
                var issue = new Issue
                {
                    MunicipalityId = user.MunicipalityId,
                    Title = InputSanitizer.SanitizeString(item.Title, 200),
                    Description = InputSanitizer.SanitizeString(item.Description, 2000),
                    Type = item.Type,
                    Severity = item.Severity,
                    Status = Core.Enums.IssueStatus.Reported,
                    ReportedByUserId = userId.Value,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    LocationDescription = item.LocationDescription,
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
            Issues = issueDtos
        }));
    }
}
