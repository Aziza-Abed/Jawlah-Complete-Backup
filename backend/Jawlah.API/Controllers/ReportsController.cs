using System.Text;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class ReportsController : ControllerBase
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly JawlahDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IAttendanceRepository attendanceRepo, ITaskRepository taskRepo, JawlahDbContext context, ILogger<ReportsController> logger)
    {
        _attendanceRepo = attendanceRepo;
        _taskRepo = taskRepo;
        _context = context;
        _logger = logger;
    }

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
            var records = await _attendanceRepo.GetFilteredAttendanceAsync(
                workerId, zoneId, startDate, endDate);

            if (format.ToLower() == "csv")
            {
                var csv = GenerateAttendanceCsv(records);
                var fileName = $"attendance_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }

            var response = records.Select(a => new
            {
                a.AttendanceId,
                a.UserId,
                WorkerName = a.User?.FullName,
                a.ZoneId,
                ZoneName = a.Zone?.ZoneName,
                a.CheckInEventTime,
                a.CheckOutEventTime,
                a.CheckInLatitude,
                a.CheckInLongitude,
                a.CheckOutLatitude,
                a.CheckOutLongitude,
                WorkDuration = a.WorkDuration?.ToString(@"hh\:mm\:ss"),
                Status = a.Status.ToString(),
                a.IsValidated
            });

            return Ok(ApiResponse<object>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance report");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to generate report"));
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
            var tasks = await _taskRepo.GetFilteredTasksAsync(
                workerId, zoneId, startDate, endDate, status);

            if (format.ToLower() == "csv")
            {
                var csv = GenerateTasksCsv(tasks);
                var fileName = $"tasks_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }

            var response = tasks.Select(t => new
            {
                t.TaskId,
                t.Title,
                t.Description,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToName = t.AssignedToUser?.FullName,
                AssignedByUserId = t.AssignedByUserId,
                AssignedByName = t.AssignedByUser?.FullName,
                t.ZoneId,
                ZoneName = t.Zone?.ZoneName,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                t.DueDate,
                t.CompletedAt,
                t.CreatedAt
            });

            return Ok(ApiResponse<object>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tasks report");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to generate report"));
        }
    }

    private static string GenerateAttendanceCsv(IEnumerable<Core.Entities.Attendance> records)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("AttendanceId,UserId,WorkerName,ZoneId,ZoneName,CheckInTime,CheckOutTime,CheckInLat,CheckInLng,CheckOutLat,CheckOutLng,WorkDuration,Status,IsValidated");

        // Data rows
        foreach (var a in records)
        {
            sb.AppendLine($"{a.AttendanceId},{a.UserId},{EscapeCsv(a.User?.FullName)},{a.ZoneId},{EscapeCsv(a.Zone?.ZoneName)},{a.CheckInEventTime:yyyy-MM-dd HH:mm:ss},{a.CheckOutEventTime?.ToString("yyyy-MM-dd HH:mm:ss")},{a.CheckInLatitude},{a.CheckInLongitude},{a.CheckOutLatitude},{a.CheckOutLongitude},{a.WorkDuration?.ToString(@"hh\:mm\:ss")},{a.Status},{a.IsValidated}");
        }

        return sb.ToString();
    }

    private static string GenerateTasksCsv(IEnumerable<Core.Entities.Task> tasks)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("TaskId,Title,Description,AssignedToUserId,AssignedToName,AssignedByUserId,AssignedByName,ZoneId,ZoneName,Priority,Status,DueDate,CompletedAt,CreatedAt");

        // Data rows
        foreach (var t in tasks)
        {
            sb.AppendLine($"{t.TaskId},{EscapeCsv(t.Title)},{EscapeCsv(t.Description)},{t.AssignedToUserId},{EscapeCsv(t.AssignedToUser?.FullName)},{t.AssignedByUserId},{EscapeCsv(t.AssignedByUser?.FullName)},{t.ZoneId},{EscapeCsv(t.Zone?.ZoneName)},{t.Priority},{t.Status},{t.DueDate:yyyy-MM-dd HH:mm:ss},{t.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss")},{t.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
