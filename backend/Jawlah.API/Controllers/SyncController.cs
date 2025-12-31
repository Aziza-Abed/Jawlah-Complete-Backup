using AutoMapper;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Sync;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class SyncController : BaseApiController
{
    private readonly ITaskRepository _tasks;
    private readonly IAttendanceRepository _attendance;
    private readonly IIssueRepository _issues;
    private readonly IMapper _mapper;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        ITaskRepository tasks,
        IAttendanceRepository attendance,
        IIssueRepository issues,
        IMapper mapper,
        ILogger<SyncController> logger)
    {
        _tasks = tasks;
        _attendance = attendance;
        _issues = issues;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost("attendance/batch")]
    public async Task<IActionResult> SyncAttendanceBatch([FromBody] BatchSyncRequest<AttendanceSyncDto> request)
    {
        // 1. check current user
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var results = new List<SyncResult>();

        // 2. process each attendance item from the mobile app
        foreach (var item in request.Items)
        {
            try
            {
                // check if already exists for today
                var existing = await _attendance.GetTodayAttendanceAsync(userId.Value);
                if (existing != null)
                {
                    // update existing
                    existing.CheckOutEventTime = item.CheckOutTime;
                    existing.CheckOutLatitude = item.CheckOutLatitude;
                    existing.CheckOutLongitude = item.CheckOutLongitude;
                    existing.Status = item.CheckOutTime.HasValue ? Core.Enums.AttendanceStatus.CheckedOut : Core.Enums.AttendanceStatus.CheckedIn;
                    existing.SyncVersion++;
                    existing.CheckOutSyncTime = DateTime.UtcNow;

                    await _attendance.UpdateAsync(existing);
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = existing.AttendanceId, Success = true });
                }
                else
                {
                    // create new
                    var attendance = new Attendance
                    {
                        UserId = userId.Value,
                        CheckInEventTime = item.CheckInTime,
                        CheckOutEventTime = item.CheckOutTime,
                        CheckInLatitude = item.CheckInLatitude,
                        CheckInLongitude = item.CheckInLongitude,
                        CheckOutLatitude = item.CheckOutLatitude,
                        CheckOutLongitude = item.CheckOutLongitude,
                        IsValidated = item.IsValidated,
                        ValidationMessage = item.ValidationMessage,
                        Status = item.CheckOutTime.HasValue ? Core.Enums.AttendanceStatus.CheckedOut : Core.Enums.AttendanceStatus.CheckedIn,
                        IsSynced = true,
                        SyncVersion = 1,
                        CheckInSyncTime = DateTime.UtcNow,
                        CheckOutSyncTime = item.CheckOutTime.HasValue ? DateTime.UtcNow : null
                    };

                    await _attendance.AddAsync(attendance);
                    await _attendance.SaveChangesAsync();
                    results.Add(new SyncResult { ClientId = item.ClientId, ServerId = attendance.AttendanceId, Success = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing attendance item");
                results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = ex.Message });
            }
        }

        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse { Results = results }));
    }

    [HttpPost("tasks/batch")]
    public async Task<IActionResult> SyncTasksBatch([FromBody] BatchSyncRequest<TaskSyncDto> request)
    {
        // 1. check current user
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var results = new List<SyncResult>();

        // 2. process each task update from the mobile app
        foreach (var item in request.Items)
        {
            try
            {
                var task = await _tasks.GetByIdAsync(item.TaskId);
                if (task == null)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "task not found" });
                    continue;
                }

                if (task.AssignedToUserId != userId.Value)
                {
                    results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = "unauthorized" });
                    continue;
                }

                // update task status and details
                task.Status = item.Status;
                task.CompletionNotes = item.CompletionNotes;
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
                _logger.LogError(ex, "Error syncing task item");
                results.Add(new SyncResult { ClientId = item.ClientId, Success = false, Message = ex.Message });
            }
        }

        await _tasks.SaveChangesAsync();
        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse { Results = results }));
    }

    [HttpGet("changes")]
    public async Task<IActionResult> GetChanges([FromQuery] DateTime lastSyncTime)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        // fetch changes for worker since last sync
        var tasks = await _tasks.GetUserTasksAsync(userId.Value);
        var filteredTasks = tasks.Where(t => t.SyncTime > lastSyncTime);

        var issues = await _issues.GetUserIssuesAsync(userId.Value);
        var filteredIssues = issues.Where(i => i.SyncTime > lastSyncTime);

        var taskDtos = _mapper.Map<List<TaskSyncDto>>(filteredTasks);
        var issueDtos = _mapper.Map<List<IssueSyncDto>>(filteredIssues);
        
        return Ok(ApiResponse<SyncChangesResponse>.SuccessResponse(new SyncChangesResponse
        {
            Tasks = taskDtos,
            Issues = issueDtos
        }));
    }
}
