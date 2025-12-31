using System.Text;
using Jawlah.Core.DTOs.Common;
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
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IAttendanceRepository attendance, ITaskRepository tasks, ILogger<ReportsController> logger)
    {
        _attendance = attendance;
        _tasks = tasks;
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
            // 1. get all attendance records from the repo based on the search filters
            var records = await _attendance.GetFilteredAttendanceAsync(
                workerId, zoneId, startDate, endDate);

            // 2. if the user wants a CSV file
            if (format.ToLower() == "csv")
            {
                var sb = new StringBuilder();
                // add the header row
                sb.AppendLine("AttendanceId,UserId,WorkerName,ZoneId,ZoneName,CheckInTime,CheckOutTime,CheckInLat,CheckInLng,CheckOutLat,CheckOutLng,WorkDuration,Status,IsValidated");

                // add each row of data
                foreach (var a in records)
                {
                    sb.AppendLine($"{a.AttendanceId},{a.UserId},{cleanForCsv(a.User?.FullName)},{a.ZoneId},{cleanForCsv(a.Zone?.ZoneName)},{a.CheckInEventTime:yyyy-MM-dd HH:mm:ss},{a.CheckOutEventTime?.ToString("yyyy-MM-dd HH:mm:ss")},{a.CheckInLatitude},{a.CheckInLongitude},{a.CheckOutLatitude},{a.CheckOutLongitude},{a.WorkDuration?.ToString(@"hh\:mm\:ss")},{a.Status},{a.IsValidated}");
                }

                var csvResult = sb.ToString();
                var fileName = $"attendance_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return File(Encoding.UTF8.GetBytes(csvResult), "text/csv", fileName);
            }

            // 3. if the user wants JSON (default)
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

            return Ok(ApiResponse<object>.SuccessResponse(response));
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
            // 1. fetch tasks based on filters
            var filteredTasks = await _tasks.GetFilteredTasksAsync(
                workerId, zoneId, startDate, endDate, status);

            // 2. format output based on choice
            if (format.ToLower() == "csv")
            {
                var sb = new StringBuilder();
                // table header
                sb.AppendLine("TaskId,Title,Description,AssignedToUserId,AssignedToName,AssignedByUserId,AssignedByName,ZoneId,ZoneName,Priority,Status,DueDate,CompletedAt,CreatedAt");

                // table data
                foreach (var t in filteredTasks)
                {
                    sb.AppendLine($"{t.TaskId},{cleanForCsv(t.Title)},{cleanForCsv(t.Description)},{t.AssignedToUserId},{cleanForCsv(t.AssignedToUser?.FullName)},{t.AssignedByUserId},{cleanForCsv(t.AssignedByUser?.FullName)},{t.ZoneId},{cleanForCsv(t.Zone?.ZoneName)},{t.Priority},{t.Status},{t.DueDate:yyyy-MM-dd HH:mm:ss},{t.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss")},{t.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }

                var csvResult = sb.ToString();
                var fileName = $"tasks_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return File(Encoding.UTF8.GetBytes(csvResult), "text/csv", fileName);
            }

            var response = filteredTasks.Select(t => new
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

            return Ok(ApiResponse<object>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tasks report");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to generate report"));
        }
    }


    private static string cleanForCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
