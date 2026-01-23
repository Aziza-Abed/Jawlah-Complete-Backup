using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;

namespace FollowUp.API.Controllers;

// this controller provide dashboard stats for supervisors
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class DashboardController : BaseApiController
{
    private readonly IUserRepository _users;
    private readonly IAttendanceRepository _attendance;
    private readonly ITaskRepository _tasks;
    private readonly IIssueRepository _issues;

    public DashboardController(
        IUserRepository users,
        IAttendanceRepository attendance,
        ITaskRepository tasks,
        IIssueRepository issues)
    {
        _users = users;
        _attendance = attendance;
        _tasks = tasks;
        _issues = issues;
    }

    // get overview stats for today (filtered by supervisor's workers if not admin)
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var today = DateTime.UtcNow.Date;

        // get all workers
        var workers = await _users.GetByRoleAsync(UserRole.Worker);

        // SECURITY FIX: Supervisors can only see their own workers' stats
        var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var currentUserId = GetCurrentUserId();
        if (currentRole?.Equals("Supervisor", StringComparison.OrdinalIgnoreCase) == true && currentUserId.HasValue)
        {
            workers = workers.Where(w => w.SupervisorId == currentUserId.Value);
        }

        var activeWorkers = workers.Where(w => w.Status == UserStatus.Active).ToList();

        // Get the set of worker IDs for filtering
        var workerIds = activeWorkers.Select(w => w.UserId).ToList();

        // PERFORMANCE FIX: Get only today's attendance for specific workers
        var todayAttendance = await _attendance.GetTodayAttendanceForWorkersAsync(workerIds);

        // count who checked in and out
        var checkedIn = todayAttendance.Where(a => a.Status == AttendanceStatus.CheckedIn).ToList();
        var checkedOut = todayAttendance.Where(a => a.Status == AttendanceStatus.CheckedOut).ToList();

        // PERFORMANCE FIX: Use database-level aggregation instead of loading all entities
        var taskStats = await _tasks.GetTaskStatsAsync(workerIds, today);
        var issueStats = await _issues.GetIssueStatsAsync(workerIds, today);

        // calculate workers not checked in
        var workersWithAttendance = todayAttendance.Select(a => a.UserId).Distinct().Count();
        var notCheckedInCount = Math.Max(0, activeWorkers.Count - workersWithAttendance);

        // build response
        var overview = new
        {
            Workers = new
            {
                Total = activeWorkers.Count,
                CheckedIn = checkedIn.Count,
                CheckedOut = checkedOut.Count,
                NotCheckedIn = notCheckedInCount
            },
            Tasks = new
            {
                CreatedToday = taskStats.CreatedToday,
                Pending = taskStats.Pending,
                InProgress = taskStats.InProgress,
                CompletedToday = taskStats.CompletedToday
            },
            Issues = new
            {
                ReportedToday = issueStats.ReportedToday,
                Unresolved = issueStats.Unresolved
            },
            Date = today
        };

        return Ok(ApiResponse<object>.SuccessResponse(overview));
    }

    // get status of all workers (filtered by supervisor's workers if not admin)
    [HttpGet("worker-status")]
    public async Task<IActionResult> GetWorkerStatus()
    {
        var today = DateTime.UtcNow.Date;

        // get active workers
        var workers = await _users.GetByRoleAsync(UserRole.Worker);

        // SECURITY FIX: Supervisors can only see their own workers' status
        var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var currentUserId = GetCurrentUserId();
        if (currentRole?.Equals("Supervisor", StringComparison.OrdinalIgnoreCase) == true && currentUserId.HasValue)
        {
            workers = workers.Where(w => w.SupervisorId == currentUserId.Value);
        }

        var activeWorkers = workers.Where(w => w.Status == UserStatus.Active).ToList();

        // PERFORMANCE FIX: Get only today's attendance for specific workers
        var workerIds = activeWorkers.Select(w => w.UserId).ToList();
        var todayAttendance = (await _attendance.GetTodayAttendanceForWorkersAsync(workerIds)).ToList();

        // PERFORMANCE FIX: Get only needed task data for these workers
        var allTasks = (await _tasks.GetTasksForWorkersAsync(workerIds)).ToList();

        // create lookups to avoid repeated loops
        var attendanceByUser = todayAttendance.GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());
        var tasksByUser = allTasks.GroupBy(t => t.AssignedToUserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // build worker status list
        var workerStatuses = activeWorkers.Select(w =>
        {
            attendanceByUser.TryGetValue(w.UserId, out var attendance);
            tasksByUser.TryGetValue(w.UserId, out var workerTasks);
            workerTasks ??= new List<Core.Entities.Task>();

            return new
            {
                UserId = w.UserId,
                FullName = w.FullName,
                EmployeeId = w.Username,
                Status = attendance?.Status.ToString() ?? "NotCheckedIn",
                CheckInTime = attendance?.CheckInEventTime,
                ZoneName = attendance?.Zone?.ZoneName,
                TodayTasksCount = workerTasks.Count(t => t.CreatedAt >= today),
                PendingTasksCount = workerTasks.Count(t => t.Status == TaskStatus.Pending),
                CompletedTasksCount = workerTasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved)
            };
        });

        return Ok(ApiResponse<object>.SuccessResponse(workerStatuses));
    }
}
