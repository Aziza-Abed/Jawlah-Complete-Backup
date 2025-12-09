using System.Security.Claims;
using Jawlah.API.Models;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Sync;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class SyncController : BaseApiController
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly IIssueRepository _issueRepo;
    private readonly JawlahDbContext _context;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IAttendanceRepository attendanceRepo,
        ITaskRepository taskRepo,
        IIssueRepository issueRepo,
        JawlahDbContext context,
        ILogger<SyncController> logger)
    {
        _attendanceRepo = attendanceRepo;
        _taskRepo = taskRepo;
        _issueRepo = issueRepo;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Batch upload attendance records from offline device
    /// </summary>
    [HttpPost("attendance/batch")]
    public async Task<IActionResult> SyncAttendanceBatch([FromBody] BatchSyncRequest<AttendanceSyncDto> request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var results = new List<SyncResult>();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in request.Items)
            {
                try
                {
                    // Check for duplicate check-in
                    var existingAttendance = await _attendanceRepo.GetTodayAttendanceAsync(item.UserId);

                    if (existingAttendance != null)
                    {
                        // Duplicate - server wins
                        results.Add(new SyncResult
                        {
                            ClientId = item.ClientId,
                            Success = false,
                            Message = "Duplicate check-in - server record kept",
                            ConflictResolution = "ServerWins"
                        });
                        continue;
                    }

                    var attendance = new Attendance
                    {
                        UserId = item.UserId,
                        CheckInEventTime = item.CheckInTime,
                        CheckOutEventTime = item.CheckOutTime,
                        CheckInLatitude = item.CheckInLatitude,
                        CheckInLongitude = item.CheckInLongitude,
                        CheckOutLatitude = item.CheckOutLatitude,
                        CheckOutLongitude = item.CheckOutLongitude,
                        IsValidated = item.IsValidated,
                        ValidationMessage = item.ValidationMessage,
                        CheckInSyncTime = DateTime.UtcNow,
                        CheckOutSyncTime = item.CheckOutTime != null ? DateTime.UtcNow : null,
                        IsSynced = true,
                        SyncVersion = 1,
                        Status = item.CheckOutTime != null ? AttendanceStatus.CheckedOut : AttendanceStatus.CheckedIn
                    };

                    await _attendanceRepo.AddAsync(attendance);

                    results.Add(new SyncResult
                    {
                        ClientId = item.ClientId,
                        ServerId = attendance.AttendanceId,
                        Success = true,
                        Message = "Synced successfully"
                    });

                    _logger.LogInformation("Attendance synced from offline device for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing attendance for user {UserId}", userId);
                    results.Add(new SyncResult
                    {
                        ClientId = item.ClientId,
                        Success = false,
                        Message = $"Error: {ex.Message}"
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Batch attendance sync failed, rolled back");
            throw;
        }

        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse
        {
            TotalItems = request.Items.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        }));
    }

    /// <summary>
    /// Batch upload task updates from offline device
    /// </summary>
    [HttpPost("tasks/batch")]
    public async Task<IActionResult> SyncTasksBatch([FromBody] BatchSyncRequest<TaskSyncDto> request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var results = new List<SyncResult>();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in request.Items)
            {
                try
                {
                    var task = await _taskRepo.GetByIdAsync(item.TaskId);
                    if (task == null)
                    {
                        results.Add(new SyncResult
                        {
                            ClientId = item.ClientId,
                            Success = false,
                            Message = "Task not found"
                        });
                        continue;
                    }

                    // Conflict resolution: Server version wins if versions mismatch
                    if (task.SyncVersion > item.SyncVersion)
                    {
                        results.Add(new SyncResult
                        {
                            ClientId = item.ClientId,
                            ServerId = task.TaskId,
                            Success = false,
                            Message = "Conflict - server version is newer",
                            ConflictResolution = "ServerWins",
                            ServerVersion = task.SyncVersion
                        });
                        continue;
                    }

                    // Update task
                    task.Status = item.Status;
                    task.CompletionNotes = item.CompletionNotes;
                    task.PhotoUrl = item.PhotoUrl;
                    task.CompletedAt = item.CompletedAt;
                    task.SyncTime = DateTime.UtcNow;
                    task.SyncVersion++;

                    await _taskRepo.UpdateAsync(task);

                    results.Add(new SyncResult
                    {
                        ClientId = item.ClientId,
                        ServerId = task.TaskId,
                        Success = true,
                        Message = "Synced successfully",
                        ServerVersion = task.SyncVersion
                    });

                    _logger.LogInformation("Task {TaskId} synced from offline device", task.TaskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing task {TaskId}", item.TaskId);
                    results.Add(new SyncResult
                    {
                        ClientId = item.ClientId,
                        Success = false,
                        Message = $"Error: {ex.Message}"
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Batch task sync failed, rolled back");
            throw;
        }

        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse
        {
            TotalItems = request.Items.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        }));
    }

    /// <summary>
    /// Batch upload issues from offline device
    /// </summary>
    [HttpPost("issues/batch")]
    public async Task<IActionResult> SyncIssuesBatch([FromBody] BatchSyncRequest<IssueSyncDto> request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var results = new List<SyncResult>();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in request.Items)
            {
                try
                {
                    var issue = new Issue
                    {
                        Title = item.Title,
                        Description = item.Description,
                        Type = item.Type,
                        Severity = item.Severity,
                        ReportedByUserId = item.ReportedByUserId,
                        Latitude = item.Latitude,
                        Longitude = item.Longitude,
                        LocationDescription = item.LocationDescription,
                        PhotoUrl = item.PhotoUrl,
                        Status = IssueStatus.Reported,
                        ReportedAt = item.ReportedAt,
                        EventTime = item.ReportedAt,
                        SyncTime = DateTime.UtcNow,
                        IsSynced = true,
                        SyncVersion = 1
                    };

                    await _issueRepo.AddAsync(issue);

                    results.Add(new SyncResult
                    {
                        ClientId = item.ClientId,
                        ServerId = issue.IssueId,
                        Success = true,
                        Message = "Synced successfully"
                    });

                    _logger.LogInformation("Issue synced from offline device");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing issue");
                    results.Add(new SyncResult
                    {
                        ClientId = item.ClientId,
                        Success = false,
                        Message = $"Error: {ex.Message}"
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Batch issue sync failed, rolled back");
            throw;
        }

        return Ok(ApiResponse<BatchSyncResponse>.SuccessResponse(new BatchSyncResponse
        {
            TotalItems = request.Items.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        }));
    }

    /// <summary>
    /// Get all changes since last sync time
    /// </summary>
    [HttpGet("changes")]
    public async Task<IActionResult> GetChangesSinceLastSync([FromQuery] DateTime? lastSyncTime)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var syncTime = lastSyncTime ?? DateTime.UtcNow.AddDays(-7);

        // Get all tasks assigned to user that changed since last sync
        var tasks = await _taskRepo.GetUserTasksAsync(userId.Value);
        var changedTasks = tasks.Where(t => t.SyncTime > syncTime).ToList();

        // Get user's issues that changed
        var issues = await _issueRepo.GetUserIssuesAsync(userId.Value);
        var changedIssues = issues.Where(i => i.SyncTime > syncTime).ToList();

        var response = new SyncChangesResponse
        {
            LastSyncTime = syncTime,
            CurrentServerTime = DateTime.UtcNow,
            Tasks = changedTasks.Select(t => new TaskSyncDto
            {
                TaskId = t.TaskId,
                Title = t.Title,
                Status = t.Status,
                CompletionNotes = t.CompletionNotes,
                PhotoUrl = t.PhotoUrl,
                CompletedAt = t.CompletedAt,
                SyncVersion = t.SyncVersion
            }).ToList(),
            Issues = changedIssues.Select(i => new IssueSyncDto
            {
                IssueId = i.IssueId,
                Title = i.Title,
                Description = i.Description,
                Type = i.Type,
                Severity = i.Severity,
                Status = i.Status,
                ReportedByUserId = i.ReportedByUserId,
                Latitude = i.Latitude,
                Longitude = i.Longitude,
                PhotoUrl = i.PhotoUrl,
                ReportedAt = i.ReportedAt,
                SyncVersion = i.SyncVersion
            }).ToList()
        };

        return Ok(ApiResponse<SyncChangesResponse>.SuccessResponse(response));
    }
}
