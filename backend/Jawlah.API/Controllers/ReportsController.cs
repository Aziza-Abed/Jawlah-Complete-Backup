using System.Text;
using ClosedXML.Excel;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Reports;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class ReportsController : ControllerBase
{
    private readonly IAttendanceRepository _attendance;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IAttendanceRepository attendance,
        ITaskRepository tasks,
        IUserRepository users,
        IZoneRepository zones,
        ILogger<ReportsController> logger)
    {
        _attendance = attendance;
        _tasks = tasks;
        _users = users;
        _zones = zones;
        _logger = logger;
    }

    // ========== SUMMARY ENDPOINTS ==========

    [HttpGet("tasks/summary")]
    public async Task<IActionResult> GetTasksReportSummary(
        [FromQuery] string period = "monthly",
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var (fromDate, toDate) = GetDateRange(period, startDate, endDate);

            TaskStatus? statusFilter = status?.ToLower() switch
            {
                "pending" => TaskStatus.Pending,
                "in_progress" => TaskStatus.InProgress,
                "completed" => TaskStatus.Completed,
                _ => null
            };

            var tasks = (await _tasks.GetFilteredTasksAsync(null, null, fromDate, toDate, statusFilter)).ToList();
            var workers = (await _users.GetByRoleAsync(UserRole.Worker)).ToList();
            var todayAttendance = await _attendance.GetFilteredAttendanceAsync(null, null, DateTime.Today, DateTime.Today.AddDays(1));

            var data = new TasksReportData
            {
                Total = tasks.Count,
                Completed = tasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved),
                InProgress = tasks.Count(t => t.Status == TaskStatus.InProgress),
                Pending = tasks.Count(t => t.Status == TaskStatus.Pending),
                Cancelled = tasks.Count(t => t.Status == TaskStatus.Cancelled),
                ActiveWorkers = todayAttendance.Select(a => a.UserId).Distinct().Count(),
                TotalWorkers = workers.Count,
                ByPeriod = BuildTasksByPeriod(tasks, period),
                Tasks = tasks.OrderByDescending(t => t.CreatedAt).Take(50).Select(t => new TaskItem
                {
                    Id = t.TaskId,
                    Title = t.Title,
                    Worker = t.AssignedToUser?.FullName ?? "",
                    Zone = t.Zone?.ZoneName ?? "",
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt
                }).ToList()
            };

            return Ok(ApiResponse<TasksReportData>.SuccessResponse(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tasks report");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء التقرير"));
        }
    }

    [HttpGet("workers/summary")]
    public async Task<IActionResult> GetWorkersReportSummary(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var (fromDate, toDate) = GetDateRange(period, startDate, endDate);

            var workers = (await _users.GetByRoleAsync(UserRole.Worker)).ToList();
            var attendance = (await _attendance.GetFilteredAttendanceAsync(null, null, fromDate, toDate)).ToList();
            var tasks = (await _tasks.GetFilteredTasksAsync(null, null, fromDate, toDate, null)).ToList();

            var todayAttendance = attendance.Where(a => a.CheckInEventTime.Date == DateTime.Today).ToList();
            var checkedInIds = todayAttendance.Select(a => a.UserId).Distinct().ToHashSet();

            // compliance = actual attendance days / expected work days
            int workDays = GetWorkDays(fromDate, toDate);
            int totalExpected = workers.Count * workDays;
            int actualDays = attendance.Select(a => new { a.UserId, a.CheckInEventTime.Date }).Distinct().Count();
            int compliance = totalExpected > 0 ? (int)Math.Round((double)actualDays / totalExpected * 100) : 0;

            var data = new WorkersReportData
            {
                TotalWorkers = workers.Count,
                CheckedIn = checkedInIds.Count,
                Absent = workers.Count - checkedInIds.Count,
                CompliancePercent = compliance,
                ByPeriod = BuildAttendanceByPeriod(attendance, workers.Count, period),
                TopWorkload = tasks
                    .Where(t => t.Status == TaskStatus.InProgress || t.Status == TaskStatus.Pending)
                    .GroupBy(t => t.AssignedToUserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .Select(x => new WorkerWorkload
                    {
                        UserId = x.UserId,
                        Name = workers.FirstOrDefault(w => w.UserId == x.UserId)?.FullName ?? "",
                        ActiveTasks = x.Count
                    }).ToList(),
                Workers = workers.Select(w =>
                {
                    var workerTasks = tasks.Where(t => t.AssignedToUserId == w.UserId).ToList();
                    var lastCheckIn = todayAttendance.FirstOrDefault(a => a.UserId == w.UserId);
                    return new WorkerItem
                    {
                        Id = w.UserId,
                        Name = w.FullName,
                        IsPresent = checkedInIds.Contains(w.UserId),
                        LastCheckIn = lastCheckIn?.CheckInEventTime,
                        ActiveTasks = workerTasks.Count(t => t.Status == TaskStatus.InProgress || t.Status == TaskStatus.Pending),
                        CompletedTasks = workerTasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved)
                    };
                }).ToList()
            };

            return Ok(ApiResponse<WorkersReportData>.SuccessResponse(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating workers report");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء التقرير"));
        }
    }

    [HttpGet("zones/summary")]
    public async Task<IActionResult> GetZonesReportSummary(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var (fromDate, toDate) = GetDateRange(period, startDate, endDate);

            var zones = (await _zones.GetActiveZonesAsync()).ToList();
            var tasks = (await _tasks.GetFilteredTasksAsync(null, null, fromDate, toDate, null)).ToList();

            var tasksByZone = tasks.Where(t => t.ZoneId.HasValue)
                .GroupBy(t => t.ZoneId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var zoneItems = zones.Select(z =>
            {
                var zoneTasks = tasksByZone.GetValueOrDefault(z.ZoneId) ?? new List<Core.Entities.Task>();
                return new ZoneItem
                {
                    Id = z.ZoneId,
                    Name = z.ZoneName,
                    Total = zoneTasks.Count,
                    Completed = zoneTasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved),
                    InProgress = zoneTasks.Count(t => t.Status == TaskStatus.InProgress),
                    Delayed = zoneTasks.Count(t => t.DueDate < DateTime.Now &&
                        t.Status != TaskStatus.Completed && t.Status != TaskStatus.Approved && t.Status != TaskStatus.Cancelled),
                    LastUpdate = zoneTasks.Any() ? zoneTasks.Max(t => t.CompletedAt ?? t.CreatedAt) : null
                };
            }).ToList();

            var highestPressure = zoneItems.OrderByDescending(z => z.Total).FirstOrDefault();

            var data = new ZonesReportData
            {
                TotalZones = zones.Count,
                TotalTasks = tasks.Count,
                TotalCompleted = tasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved),
                TotalInProgress = tasks.Count(t => t.Status == TaskStatus.InProgress),
                TotalDelayed = tasks.Count(t => t.DueDate < DateTime.Now &&
                    t.Status != TaskStatus.Completed && t.Status != TaskStatus.Approved && t.Status != TaskStatus.Cancelled),
                HighestPressureZone = highestPressure?.Name ?? "",
                Zones = zoneItems
            };

            return Ok(ApiResponse<ZonesReportData>.SuccessResponse(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating zones report");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء التقرير"));
        }
    }

    // ========== INDIVIDUAL WORKER REPORT ==========

    /// <summary>
    /// Get detailed performance report for a specific worker
    /// </summary>
    [HttpGet("worker/{workerId}")]
    public async Task<IActionResult> GetIndividualWorkerReport(
        int workerId,
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var worker = await _users.GetByIdAsync(workerId);
            if (worker == null)
                return NotFound(ApiResponse<object>.ErrorResponse("العامل غير موجود"));

            if (worker.Role != UserRole.Worker)
                return BadRequest(ApiResponse<object>.ErrorResponse("هذا التقرير للعمال فقط"));

            var (fromDate, toDate) = GetDateRange(period, startDate, endDate);

            // Get worker's attendance
            var attendance = (await _attendance.GetFilteredAttendanceAsync(workerId, null, fromDate, toDate)).ToList();

            // Get worker's tasks
            var tasks = (await _tasks.GetFilteredTasksAsync(workerId, null, fromDate, toDate, null)).ToList();

            // Calculate work days and attendance stats
            int workDays = GetWorkDays(fromDate, toDate);
            int daysPresent = attendance.Select(a => a.CheckInEventTime.Date).Distinct().Count();
            int daysAbsent = workDays - daysPresent;

            // Calculate lateness
            int lateDays = attendance.Count(a => a.LateMinutes > 0);
            var totalOvertimeMinutes = attendance
                .Where(a => a.OvertimeMinutes > 0)
                .Sum(a => a.OvertimeMinutes);

            // Calculate average work duration
            var totalWorkMinutes = attendance
                .Where(a => a.WorkDuration.HasValue)
                .Sum(a => a.WorkDuration!.Value.TotalMinutes);
            var avgWorkHours = daysPresent > 0 ? totalWorkMinutes / daysPresent / 60 : 0;

            // Task statistics
            var completedTasks = tasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved);
            var rejectedTasks = tasks.Count(t => t.Status == TaskStatus.Rejected);
            var autoRejectedTasks = tasks.Count(t => t.IsAutoRejected);
            var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
            var pendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending);

            // Location warnings
            var tasksWithLocationWarnings = tasks.Count(t => t.IsDistanceWarning);

            // Build the report
            var report = new IndividualWorkerReportData
            {
                WorkerId = worker.UserId,
                WorkerName = worker.FullName,
                WorkerType = worker.WorkerType?.ToString(),
                Department = worker.Department,
                Period = period,
                FromDate = fromDate,
                ToDate = toDate,

                // Attendance Summary
                Attendance = new AttendanceSummary
                {
                    TotalWorkDays = workDays,
                    DaysPresent = daysPresent,
                    DaysAbsent = daysAbsent,
                    LateDays = lateDays,
                    AttendancePercentage = workDays > 0 ? Math.Round((double)daysPresent / workDays * 100, 1) : 0,
                    TotalOvertimeMinutes = totalOvertimeMinutes,
                    AverageWorkHours = Math.Round(avgWorkHours, 1)
                },

                // Task Summary
                Tasks = new TaskSummary
                {
                    TotalAssigned = tasks.Count,
                    Completed = completedTasks,
                    Approved = tasks.Count(t => t.Status == TaskStatus.Approved),
                    Rejected = rejectedTasks,
                    AutoRejected = autoRejectedTasks,
                    InProgress = inProgressTasks,
                    Pending = pendingTasks,
                    CompletionRate = tasks.Count > 0 ? Math.Round((double)completedTasks / tasks.Count * 100, 1) : 0,
                    LocationWarnings = tasksWithLocationWarnings
                },

                // Warning Summary
                Warnings = new WarningSummary
                {
                    TotalWarnings = worker.WarningCount,
                    LastWarningDate = worker.LastWarningAt,
                    LastWarningReason = worker.LastWarningReason
                },

                // Recent Activity (last 10 items)
                RecentTasks = tasks.OrderByDescending(t => t.CreatedAt).Take(10).Select(t => new TaskItem
                {
                    Id = t.TaskId,
                    Title = t.Title,
                    Worker = worker.FullName,
                    Zone = t.Zone?.ZoneName ?? "",
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    ProgressPercentage = t.ProgressPercentage,
                    IsAutoRejected = t.IsAutoRejected
                }).ToList(),

                RecentAttendance = attendance.OrderByDescending(a => a.CheckInEventTime).Take(10).Select(a => new AttendanceItem
                {
                    Date = a.CheckInEventTime.Date,
                    CheckIn = a.CheckInEventTime,
                    CheckOut = a.CheckOutEventTime,
                    WorkDuration = a.WorkDuration?.ToString(@"hh\:mm"),
                    IsLate = a.LateMinutes > 0,
                    LatenessMinutes = a.LateMinutes > 0 ? a.LateMinutes : null,
                    OvertimeMinutes = a.OvertimeMinutes > 0 ? a.OvertimeMinutes : null,
                    Zone = a.Zone?.ZoneName
                }).ToList()
            };

            return Ok(ApiResponse<IndividualWorkerReportData>.SuccessResponse(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating individual worker report for {WorkerId}", workerId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء التقرير"));
        }
    }

    // ========== RAW DATA EXPORT ==========

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendanceReport(
        [FromQuery] int? workerId,
        [FromQuery] int? zoneId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string format = "json")
    {
        try
        {
            var records = await _attendance.GetFilteredAttendanceAsync(workerId, zoneId, startDate, endDate);

            if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Attendance");

                ws.Cell(1, 1).Value = "رقم السجل";
                ws.Cell(1, 2).Value = "اسم العامل";
                ws.Cell(1, 3).Value = "المنطقة";
                ws.Cell(1, 4).Value = "وقت الدخول";
                ws.Cell(1, 5).Value = "وقت الخروج";
                ws.Cell(1, 6).Value = "مدة العمل";
                ws.Cell(1, 7).Value = "الحالة";
                ws.Range(1, 1, 1, 7).Style.Font.Bold = true;

                int row = 2;
                foreach (var a in records)
                {
                    ws.Cell(row, 1).Value = a.AttendanceId;
                    ws.Cell(row, 2).Value = a.User?.FullName ?? "";
                    ws.Cell(row, 3).Value = a.Zone?.ZoneName ?? "";
                    ws.Cell(row, 4).Value = a.CheckInEventTime.ToString("yyyy-MM-dd HH:mm");
                    ws.Cell(row, 5).Value = a.CheckOutEventTime?.ToString("yyyy-MM-dd HH:mm") ?? "";
                    ws.Cell(row, 6).Value = a.WorkDuration?.ToString(@"hh\:mm") ?? "";
                    ws.Cell(row, 7).Value = a.Status.ToString();
                    row++;
                }

                ws.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"attendance_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }

            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("AttendanceId,UserId,WorkerName,ZoneId,ZoneName,CheckInTime,CheckOutTime,WorkDuration,Status");
                foreach (var a in records)
                {
                    sb.AppendLine($"{a.AttendanceId},{a.UserId},{CsvEscape(a.User?.FullName)},{a.ZoneId},{CsvEscape(a.Zone?.ZoneName)},{a.CheckInEventTime:yyyy-MM-dd HH:mm},{a.CheckOutEventTime?.ToString("yyyy-MM-dd HH:mm")},{a.WorkDuration?.ToString(@"hh\:mm")},{a.Status}");
                }
                return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"attendance_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            // JSON
            return Ok(ApiResponse<object>.SuccessResponse(records.Select(a => new
            {
                a.AttendanceId,
                a.UserId,
                WorkerName = a.User?.FullName,
                a.ZoneId,
                ZoneName = a.Zone?.ZoneName,
                a.CheckInEventTime,
                a.CheckOutEventTime,
                WorkDuration = a.WorkDuration?.ToString(@"hh\:mm"),
                Status = a.Status.ToString()
            })));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance report");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء التقرير"));
        }
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasksReport(
        [FromQuery] int? workerId,
        [FromQuery] int? zoneId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] TaskStatus? status,
        [FromQuery] string format = "json")
    {
        try
        {
            var tasks = await _tasks.GetFilteredTasksAsync(workerId, zoneId, startDate, endDate, status);

            if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Tasks");

                ws.Cell(1, 1).Value = "رقم المهمة";
                ws.Cell(1, 2).Value = "العنوان";
                ws.Cell(1, 3).Value = "العامل";
                ws.Cell(1, 4).Value = "المنطقة";
                ws.Cell(1, 5).Value = "الأولوية";
                ws.Cell(1, 6).Value = "الحالة";
                ws.Cell(1, 7).Value = "تاريخ الاستحقاق";
                ws.Cell(1, 8).Value = "تاريخ الإنجاز";
                ws.Range(1, 1, 1, 8).Style.Font.Bold = true;

                int row = 2;
                foreach (var t in tasks)
                {
                    ws.Cell(row, 1).Value = t.TaskId;
                    ws.Cell(row, 2).Value = t.Title;
                    ws.Cell(row, 3).Value = t.AssignedToUser?.FullName ?? "";
                    ws.Cell(row, 4).Value = t.Zone?.ZoneName ?? "";
                    ws.Cell(row, 5).Value = t.Priority.ToString();
                    ws.Cell(row, 6).Value = t.Status.ToString();
                    ws.Cell(row, 7).Value = t.DueDate?.ToString("yyyy-MM-dd") ?? "";
                    ws.Cell(row, 8).Value = t.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
                    row++;
                }

                ws.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"tasks_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }

            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("TaskId,Title,Worker,Zone,Priority,Status,DueDate,CompletedAt,CreatedAt");
                foreach (var t in tasks)
                {
                    sb.AppendLine($"{t.TaskId},{CsvEscape(t.Title)},{CsvEscape(t.AssignedToUser?.FullName)},{CsvEscape(t.Zone?.ZoneName)},{t.Priority},{t.Status},{t.DueDate:yyyy-MM-dd},{t.CompletedAt?.ToString("yyyy-MM-dd HH:mm")},{t.CreatedAt:yyyy-MM-dd HH:mm}");
                }
                return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"tasks_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            // JSON
            return Ok(ApiResponse<object>.SuccessResponse(tasks.Select(t => new
            {
                t.TaskId,
                t.Title,
                Worker = t.AssignedToUser?.FullName,
                Zone = t.Zone?.ZoneName,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                t.DueDate,
                t.CompletedAt,
                t.CreatedAt
            })));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tasks report");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء التقرير"));
        }
    }

    // ========== HELPERS ==========

    private static (DateTime from, DateTime to) GetDateRange(string period, DateTime? start, DateTime? end)
    {
        var now = DateTime.Now;
        return period.ToLower() switch
        {
            "daily" => (now.Date.AddDays(-7), now.Date.AddDays(1)),
            "weekly" => (now.Date.AddDays(-28), now.Date.AddDays(1)),
            "monthly" => (now.Date.AddMonths(-1), now.Date.AddDays(1)),
            "yearly" => (now.Date.AddYears(-1), now.Date.AddDays(1)),
            "custom" when start.HasValue && end.HasValue => (start.Value.Date, end.Value.Date.AddDays(1)),
            _ => (now.Date.AddMonths(-1), now.Date.AddDays(1))
        };
    }

    private static List<TasksByPeriod> BuildTasksByPeriod(List<Core.Entities.Task> tasks, string period)
    {
        var labels = GetPeriodLabels(period);
        return labels.Select(label =>
        {
            var (start, end) = GetLabelDateRange(label, period);
            var periodTasks = tasks.Where(t => t.CreatedAt >= start && t.CreatedAt < end).ToList();
            return new TasksByPeriod
            {
                Label = label,
                Completed = periodTasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved),
                InProgress = periodTasks.Count(t => t.Status == TaskStatus.InProgress),
                Pending = periodTasks.Count(t => t.Status == TaskStatus.Pending)
            };
        }).ToList();
    }

    private static List<AttendanceByPeriod> BuildAttendanceByPeriod(List<Core.Entities.Attendance> attendance, int totalWorkers, string period)
    {
        var labels = GetPeriodLabels(period);
        return labels.Select(label =>
        {
            var (start, end) = GetLabelDateRange(label, period);
            var present = attendance.Where(a => a.CheckInEventTime >= start && a.CheckInEventTime < end)
                .Select(a => a.UserId).Distinct().Count();
            return new AttendanceByPeriod
            {
                Label = label,
                Present = present,
                Absent = Math.Max(0, totalWorkers - present)
            };
        }).ToList();
    }

    private static List<string> GetPeriodLabels(string period) => period.ToLower() switch
    {
        "daily" => Enumerable.Range(1, 7).Select(i => i.ToString()).ToList(),
        "weekly" => new List<string> { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" },
        "yearly" => Enumerable.Range(1, 12).Select(i => i.ToString()).ToList(),
        _ => Enumerable.Range(1, 30).Select(i => i.ToString()).ToList()
    };

    private static (DateTime start, DateTime end) GetLabelDateRange(string label, string period)
    {
        var now = DateTime.Now;

        if (period.Equals("daily", StringComparison.OrdinalIgnoreCase) && int.TryParse(label, out int day))
            return (now.Date.AddDays(-7 + day - 1), now.Date.AddDays(-7 + day));

        if (period.Equals("weekly", StringComparison.OrdinalIgnoreCase))
        {
            var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var idx = Array.IndexOf(days, label);
            if (idx < 0) idx = 0;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            return (weekStart.AddDays(idx), weekStart.AddDays(idx + 1));
        }

        if (period.Equals("yearly", StringComparison.OrdinalIgnoreCase) && int.TryParse(label, out int month) && month >= 1 && month <= 12)
            return (new DateTime(now.Year, month, 1), new DateTime(now.Year, month, 1).AddMonths(1));

        // monthly - day of month
        if (int.TryParse(label, out int dayOfMonth))
            return (now.Date.AddDays(-30 + dayOfMonth - 1), now.Date.AddDays(-30 + dayOfMonth));

        return (now.Date, now.Date.AddDays(1));
    }

    private static int GetWorkDays(DateTime from, DateTime to)
    {
        int days = 0;
        for (var d = from.Date; d < to.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Friday && d.DayOfWeek != DayOfWeek.Saturday)
                days++;
        }
        return Math.Max(1, days);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.StartsWith('=') || value.StartsWith('+') || value.StartsWith('-') || value.StartsWith('@'))
            value = "'" + value;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
