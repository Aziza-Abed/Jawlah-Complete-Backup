using Jawlah.Core.DTOs.Common;
using Jawlah.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

// UR23: Audit log viewer for administrators
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly AuditLogService _auditService;

    public AuditController(AuditLogService auditService)
    {
        _auditService = auditService;
    }

    // Get recent audit logs
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int count = 100,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null)
    {
        var logs = await _auditService.GetRecentLogsAsync(count, userId, action);

        var response = logs.Select(l => new
        {
            l.AuditLogId,
            l.UserId,
            l.Username,
            UserFullName = l.User?.FullName,
            l.Action,
            l.Details,
            l.IpAddress,
            l.CreatedAt
        });

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    // Get logs by date range
    [HttpGet("range")]
    public async Task<IActionResult> GetLogsByRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var logs = await _auditService.GetLogsByDateRangeAsync(from, to);

        var response = logs.Select(l => new
        {
            l.AuditLogId,
            l.UserId,
            l.Username,
            UserFullName = l.User?.FullName,
            l.Action,
            l.Details,
            l.IpAddress,
            l.CreatedAt
        });

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    // Get action types for filtering
    [HttpGet("actions")]
    public IActionResult GetActionTypes()
    {
        var actions = new[]
        {
            "Login",
            "LoginFailed",
            "Logout",
            "CheckIn",
            "CheckOut",
            "TaskCreated",
            "TaskUpdated",
            "TaskCompleted",
            "IssueReported",
            "IssueUpdated",
            "UserCreated",
            "UserUpdated"
        };

        return Ok(ApiResponse<object>.SuccessResponse(actions));
    }
}
