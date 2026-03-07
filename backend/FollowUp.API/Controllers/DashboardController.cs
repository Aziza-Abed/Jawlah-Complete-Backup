using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
[Tags("Dashboard")]
public class DashboardController : BaseApiController
{
    private readonly IUserRepository _users;
    private readonly IAttendanceRepository _attendance;
    private readonly ITaskRepository _tasks;
    private readonly IIssueRepository _issues;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IUserRepository users,
        IAttendanceRepository attendance,
        ITaskRepository tasks,
        IIssueRepository issues,
        ILogger<DashboardController> logger)
    {
        _users = users;
        _attendance = attendance;
        _tasks = tasks;
        _issues = issues;
        _logger = logger;
    }

    [HttpGet("overview")]
    [SwaggerOperation(Summary = "get today overview stats")]
    public async Task<IActionResult> GetOverview()
    {
        var today = DateTime.UtcNow.Date;

        // get active workers (filtered by supervisor if not admin)
        var (activeWorkers, workerIds) = await GetFilteredWorkersAsync();

        var todayAttendance = await _attendance.GetTodayAttendanceForWorkersAsync(workerIds);

        // count who checked in and out
        var checkedIn = todayAttendance.Where(a => a.Status == AttendanceStatus.CheckedIn).ToList();
        var checkedOut = todayAttendance.Where(a => a.Status == AttendanceStatus.CheckedOut).ToList();

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

    [HttpGet("worker-status")]
    [SwaggerOperation(Summary = "get all workers current status")]
    public async Task<IActionResult> GetWorkerStatus()
    {
        var today = DateTime.UtcNow.Date;

        // get active workers (filtered by supervisor if not admin)
        var (activeWorkers, workerIds) = await GetFilteredWorkersAsync();
        var todayAttendance = (await _attendance.GetTodayAttendanceForWorkersAsync(workerIds)).ToList();

        var allTasks = (await _tasks.GetTasksForWorkersAsync(workerIds)).ToList();

        // create lookups to avoid repeated loops
        var attendanceByUser = todayAttendance.GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());
        var tasksByUser = allTasks
            .Where(t => !t.IsTeamTask)
            .GroupBy(t => t.AssignedToUserId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var teamTasksByTeamId = allTasks
            .Where(t => t.IsTeamTask && t.TeamId.HasValue)
            .GroupBy(t => t.TeamId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // build worker status list
        var workerStatuses = activeWorkers.Select(w =>
        {
            attendanceByUser.TryGetValue(w.UserId, out var attendance);
            tasksByUser.TryGetValue(w.UserId, out var workerTasks);
            workerTasks ??= new List<Core.Entities.Task>();

            // also include team tasks for this worker's team
            if (w.TeamId.HasValue && teamTasksByTeamId.TryGetValue(w.TeamId.Value, out var teamTasks))
                workerTasks = workerTasks.Concat(teamTasks).ToList();

            return new
            {
                UserId = w.UserId,
                FullName = w.FullName,
                EmployeeId = w.Username,
                Status = attendance?.Status.ToString() ?? "NotCheckedIn",
                CheckInTime = attendance?.CheckInEventTime,
                ZoneName = attendance?.Zone?.ZoneName,
                TodayTasksCount = workerTasks.Count(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1)),
                PendingTasksCount = workerTasks.Count(t => t.Status == TaskStatus.Pending),
                CompletedTasksCount = workerTasks.Count(t => t.Status == TaskStatus.Completed)
            };
        });

        return Ok(ApiResponse<object>.SuccessResponse(workerStatuses));
    }

    // get active workers, filtered to supervisor's own workers if not admin
    private async Task<(List<Core.Entities.User> ActiveWorkers, List<int> WorkerIds)> GetFilteredWorkersAsync()
    {
        var workers = await _users.GetByRoleAsync(UserRole.Worker);

        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();
        if (currentRole == "Supervisor" && currentUserId.HasValue)
        {
            workers = workers.Where(w => w.SupervisorId == currentUserId.Value);
        }

        var activeWorkers = workers.Where(w => w.Status == UserStatus.Active).ToList();
        var workerIds = activeWorkers.Select(w => w.UserId).ToList();
        return (activeWorkers, workerIds);
    }
}
