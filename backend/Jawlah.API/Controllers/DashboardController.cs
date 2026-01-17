using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.API.Controllers;

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

    // get overview stats for today
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // get all workers
        var workers = await _users.GetByRoleAsync(UserRole.Worker);
        var activeWorkers = workers.Where(w => w.Status == UserStatus.Active).ToList();

        // get today attendance
        var todayAttendance = await _attendance.GetFilteredAttendanceAsync(
            userId: null,
            zoneId: null,
            fromDate: today,
            toDate: tomorrow);

        // count who checked in and out
        var checkedIn = todayAttendance.Where(a => a.Status == AttendanceStatus.CheckedIn).ToList();
        var checkedOut = todayAttendance.Where(a => a.Status == AttendanceStatus.CheckedOut).ToList();

        // get task stats
        var allTasks = await _tasks.GetAllAsync();
        var todayTasks = allTasks.Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow).ToList();
        var pendingTasks = allTasks.Count(t => t.Status == TaskStatus.Pending);
        var inProgressTasks = allTasks.Count(t => t.Status == TaskStatus.InProgress);
        var completedToday = allTasks.Count(t => t.Status == TaskStatus.Completed && t.CompletedAt >= today);

        // get issue stats
        var allIssues = await _issues.GetAllAsync();
        var todayIssues = allIssues.Where(i => i.ReportedAt >= today && i.ReportedAt < tomorrow).ToList();
        var unresolvedIssues = allIssues.Count(i => i.Status != IssueStatus.Resolved && i.Status != IssueStatus.Dismissed);

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
                CreatedToday = todayTasks.Count,
                Pending = pendingTasks,
                InProgress = inProgressTasks,
                CompletedToday = completedToday
            },
            Issues = new
            {
                ReportedToday = todayIssues.Count,
                Unresolved = unresolvedIssues
            },
            Date = today
        };

        return Ok(ApiResponse<object>.SuccessResponse(overview));
    }

    // get status of all workers
    [HttpGet("worker-status")]
    public async Task<IActionResult> GetWorkerStatus()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // get active workers
        var workers = await _users.GetByRoleAsync(UserRole.Worker);
        var activeWorkers = workers.Where(w => w.Status == UserStatus.Active).ToList();

        // get today attendance
        var todayAttendance = (await _attendance.GetFilteredAttendanceAsync(
            userId: null,
            zoneId: null,
            fromDate: today,
            toDate: tomorrow)).ToList();

        // get all tasks
        var allTasks = (await _tasks.GetAllAsync()).ToList();

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
                EmployeeId = w.Pin,
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
