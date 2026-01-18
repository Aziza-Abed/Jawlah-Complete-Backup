using ClosedXML.Excel;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Reports;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

    // ========== PDF EXPORT ENDPOINTS ==========

    /// <summary>
    /// Export attendance report as PDF
    /// </summary>
    [HttpGet("attendance/pdf")]
    public async Task<IActionResult> ExportAttendancePdf(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var (fromDate, toDate) = GetDateRange(period, startDate, endDate);
            var attendance = (await _attendance.GetFilteredAttendanceAsync(null, null, fromDate, toDate)).ToList();
            var workers = (await _users.GetByRoleAsync(UserRole.Worker)).ToList();

            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;

            // Generate PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // Header
                    page.Header().Element(c =>
                    {
                        c.Column(col =>
                        {
                            col.Item().AlignCenter().Text("تقرير الحضور والغياب").FontSize(18).Bold();
                            col.Item().AlignCenter().Text($"من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}").FontSize(12);
                            col.Item().PaddingVertical(10).LineHorizontal(1);
                        });
                    });

                    // Content
                    page.Content().Column(col =>
                    {
                        // Summary
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Text($"إجمالي العمال: {workers.Count}").FontSize(12);
                            row.RelativeItem().Text($"سجلات الحضور: {attendance.Count}").FontSize(12);
                        });

                        // Attendance table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // #
                                columns.RelativeColumn(2);   // Name
                                columns.RelativeColumn(1);   // Date
                                columns.RelativeColumn(1);   // Check-in
                                columns.RelativeColumn(1);   // Check-out
                                columns.RelativeColumn(1);   // Hours
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("الاسم");
                                header.Cell().Element(CellStyle).Text("التاريخ");
                                header.Cell().Element(CellStyle).Text("دخول");
                                header.Cell().Element(CellStyle).Text("خروج");
                                header.Cell().Element(CellStyle).Text("ساعات");

                                IContainer CellStyle(IContainer c) => c.Border(1).Padding(5).AlignCenter().Background(Colors.Grey.Lighten3);
                            });

                            // Data rows
                            int rowNumber = 1;
                            foreach (var record in attendance.OrderByDescending(a => a.CheckInEventTime).Take(100))
                            {
                                var worker = workers.FirstOrDefault(w => w.UserId == record.UserId);
                                var hours = record.CheckOutEventTime.HasValue
                                    ? (record.CheckOutEventTime.Value - record.CheckInEventTime).TotalHours
                                    : 0;

                                table.Cell().Element(DataCellStyle).Text(rowNumber.ToString());
                                table.Cell().Element(DataCellStyle).Text(worker?.FullName ?? "غير معروف");
                                table.Cell().Element(DataCellStyle).Text(record.CheckInEventTime.ToString("yyyy-MM-dd"));
                                table.Cell().Element(DataCellStyle).Text(record.CheckInEventTime.ToString("HH:mm"));
                                table.Cell().Element(DataCellStyle).Text(record.CheckOutEventTime?.ToString("HH:mm") ?? "-");
                                table.Cell().Element(DataCellStyle).Text(hours > 0 ? $"{hours:F1}" : "-");

                                rowNumber++;
                            }

                            IContainer DataCellStyle(IContainer c) => c.Border(1).Padding(5).AlignCenter();
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("تم إنشاء هذا التقرير بواسطة نظام جولة - ");
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"attendance_report_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance PDF");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء تقرير PDF"));
        }
    }

    /// <summary>
    /// Export tasks report as PDF
    /// </summary>
    [HttpGet("tasks/pdf")]
    public async Task<IActionResult> ExportTasksPdf(
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

            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;

            // Status mapping
            var statusMap = new Dictionary<TaskStatus, string>
            {
                { TaskStatus.Pending, "قيد الانتظار" },
                { TaskStatus.InProgress, "قيد التنفيذ" },
                { TaskStatus.Completed, "مكتملة" },
                { TaskStatus.Approved, "معتمدة" },
                { TaskStatus.Rejected, "مرفوضة" },
                { TaskStatus.Cancelled, "ملغاة" }
            };

            // Generate PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // Header
                    page.Header().Element(c =>
                    {
                        c.Column(col =>
                        {
                            col.Item().AlignCenter().Text("تقرير المهام").FontSize(18).Bold();
                            col.Item().AlignCenter().Text($"من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}").FontSize(12);
                            col.Item().PaddingVertical(10).LineHorizontal(1);
                        });
                    });

                    // Content
                    page.Content().Column(col =>
                    {
                        // Summary
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Text($"إجمالي المهام: {tasks.Count}").FontSize(12);
                            row.RelativeItem().Text($"مكتملة: {tasks.Count(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Approved)}").FontSize(12);
                        });

                        // Tasks table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);  // #
                                columns.RelativeColumn(2);   // Title
                                columns.RelativeColumn(1);   // Worker
                                columns.RelativeColumn(1);   // Zone
                                columns.RelativeColumn(1);   // Status
                                columns.RelativeColumn(1);   // Date
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("العنوان");
                                header.Cell().Element(CellStyle).Text("العامل");
                                header.Cell().Element(CellStyle).Text("المنطقة");
                                header.Cell().Element(CellStyle).Text("الحالة");
                                header.Cell().Element(CellStyle).Text("التاريخ");

                                IContainer CellStyle(IContainer c) => c.Border(1).Padding(5).AlignCenter().Background(Colors.Grey.Lighten3);
                            });

                            // Data rows
                            int rowNumber = 1;
                            foreach (var task in tasks.OrderByDescending(t => t.CreatedAt).Take(100))
                            {
                                table.Cell().Element(DataCellStyle).Text(rowNumber.ToString());
                                table.Cell().Element(DataCellStyle).Text(task.Title);
                                table.Cell().Element(DataCellStyle).Text(task.AssignedToUser?.FullName ?? "-");
                                table.Cell().Element(DataCellStyle).Text(task.Zone?.ZoneName ?? "-");
                                table.Cell().Element(DataCellStyle).Text(statusMap.GetValueOrDefault(task.Status, task.Status.ToString()));
                                table.Cell().Element(DataCellStyle).Text(task.CreatedAt.ToString("yyyy-MM-dd"));

                                rowNumber++;
                            }

                            IContainer DataCellStyle(IContainer c) => c.Border(1).Padding(5).AlignCenter();
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("تم إنشاء هذا التقرير بواسطة نظام جولة - ");
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var fileName = $"tasks_report_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tasks PDF");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء تقرير PDF"));
        }
    }

    // ========== EXCEL EXPORT ENDPOINTS ==========

    /// <summary>
    /// Export attendance report as Excel
    /// </summary>
    [HttpGet("attendance/excel")]
    public async Task<IActionResult> ExportAttendanceExcel(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var (fromDate, toDate) = GetDateRange(period, startDate, endDate);
            var attendance = (await _attendance.GetFilteredAttendanceAsync(null, null, fromDate, toDate)).ToList();
            var workers = (await _users.GetByRoleAsync(UserRole.Worker)).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تقرير الحضور");

            // Title
            worksheet.Cell(1, 1).Value = "تقرير الحضور والغياب";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 6).Merge();

            worksheet.Cell(2, 1).Value = $"من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}";
            worksheet.Range(2, 1, 2, 6).Merge();

            // Headers
            var headerRow = 4;
            worksheet.Cell(headerRow, 1).Value = "#";
            worksheet.Cell(headerRow, 2).Value = "الاسم";
            worksheet.Cell(headerRow, 3).Value = "التاريخ";
            worksheet.Cell(headerRow, 4).Value = "دخول";
            worksheet.Cell(headerRow, 5).Value = "خروج";
            worksheet.Cell(headerRow, 6).Value = "ساعات العمل";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            // Data
            int currentRow = headerRow + 1;
            int rowNumber = 1;
            foreach (var record in attendance.OrderByDescending(a => a.CheckInEventTime))
            {
                var worker = workers.FirstOrDefault(w => w.UserId == record.UserId);
                var hours = record.CheckOutEventTime.HasValue
                    ? (record.CheckOutEventTime.Value - record.CheckInEventTime).TotalHours
                    : 0;

                worksheet.Cell(currentRow, 1).Value = rowNumber;
                worksheet.Cell(currentRow, 2).Value = worker?.FullName ?? "غير معروف";
                worksheet.Cell(currentRow, 3).Value = record.CheckInEventTime.ToString("yyyy-MM-dd");
                worksheet.Cell(currentRow, 4).Value = record.CheckInEventTime.ToString("HH:mm");
                worksheet.Cell(currentRow, 5).Value = record.CheckOutEventTime?.ToString("HH:mm") ?? "-";
                worksheet.Cell(currentRow, 6).Value = hours > 0 ? $"{hours:F1}" : "-";

                currentRow++;
                rowNumber++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Return file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            var fileName = $"attendance_report_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance Excel");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء تقرير Excel"));
        }
    }

    /// <summary>
    /// Export tasks report as Excel
    /// </summary>
    [HttpGet("tasks/excel")]
    public async Task<IActionResult> ExportTasksExcel(
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

            // Status mapping
            var statusMap = new Dictionary<TaskStatus, string>
            {
                { TaskStatus.Pending, "قيد الانتظار" },
                { TaskStatus.InProgress, "قيد التنفيذ" },
                { TaskStatus.Completed, "مكتملة" },
                { TaskStatus.Approved, "معتمدة" },
                { TaskStatus.Rejected, "مرفوضة" },
                { TaskStatus.Cancelled, "ملغاة" }
            };

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تقرير المهام");

            // Title
            worksheet.Cell(1, 1).Value = "تقرير المهام";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 7).Merge();

            worksheet.Cell(2, 1).Value = $"من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}";
            worksheet.Range(2, 1, 2, 7).Merge();

            // Headers
            var headerRow = 4;
            worksheet.Cell(headerRow, 1).Value = "#";
            worksheet.Cell(headerRow, 2).Value = "العنوان";
            worksheet.Cell(headerRow, 3).Value = "الوصف";
            worksheet.Cell(headerRow, 4).Value = "العامل";
            worksheet.Cell(headerRow, 5).Value = "المنطقة";
            worksheet.Cell(headerRow, 6).Value = "الحالة";
            worksheet.Cell(headerRow, 7).Value = "التاريخ";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            // Data
            int currentRow = headerRow + 1;
            int rowNumber = 1;
            foreach (var task in tasks.OrderByDescending(t => t.CreatedAt))
            {
                worksheet.Cell(currentRow, 1).Value = rowNumber;
                worksheet.Cell(currentRow, 2).Value = task.Title;
                worksheet.Cell(currentRow, 3).Value = task.Description;
                worksheet.Cell(currentRow, 4).Value = task.AssignedToUser?.FullName ?? "-";
                worksheet.Cell(currentRow, 5).Value = task.Zone?.ZoneName ?? "-";
                worksheet.Cell(currentRow, 6).Value = statusMap.GetValueOrDefault(task.Status, task.Status.ToString());
                worksheet.Cell(currentRow, 7).Value = task.CreatedAt.ToString("yyyy-MM-dd");

                currentRow++;
                rowNumber++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Return file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            var fileName = $"tasks_report_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tasks Excel");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء تقرير Excel"));
        }
    }
}
