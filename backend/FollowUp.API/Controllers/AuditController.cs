using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
[Tags("Audit")]
public class AuditController : BaseApiController
{
    private readonly IAuditLogService _auditService;
    private readonly IUserRepository _users;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditLogService auditService, IUserRepository users, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _users = users;
        _logger = logger;
    }

    // filter logs by supervisor's workers if needed, then project to response
    private async Task<IActionResult> FilterAndRespond(IEnumerable<Core.Entities.AuditLog> logs)
    {
        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();

        if (currentRole == "Supervisor" && currentUserId.HasValue)
        {
            var workers = await _users.GetWorkersBySupervisorAsync(currentUserId.Value);
            var allowedIds = workers.Select(w => w.UserId).ToHashSet();
            allowedIds.Add(currentUserId.Value);
            logs = logs.Where(l => l.UserId.HasValue && allowedIds.Contains(l.UserId.Value));
        }

        var response = logs.Select(l => new
        {
            l.AuditLogId,
            l.UserId,
            l.Username,
            UserFullName = l.User?.FullName,
            l.Action,
            l.Details,
            l.IpAddress,
            l.UserAgent,
            l.CreatedAt
        });

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    [HttpGet]
    [SwaggerOperation(Summary = "get recent audit logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int count = 100,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null)
    {
        count = Math.Clamp(count, 1, 500);
        var logs = await _auditService.GetRecentLogsAsync(count, userId, action);
        return await FilterAndRespond(logs);
    }

    [HttpGet("range")]
    [SwaggerOperation(Summary = "get audit logs by date range")]
    public async Task<IActionResult> GetLogsByRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var logs = await _auditService.GetLogsByDateRangeAsync(from, to);
        return await FilterAndRespond(logs);
    }

    [HttpGet("actions")]
    [SwaggerOperation(Summary = "get available action types for filtering")]
    public IActionResult GetActionTypes()
    {
        var actions = new[]
        {
            "Login",
            "Login2FA",
            "LoginFailed",
            "Logout",
            "CheckIn",
            "CheckOut",
            "ManualAttendanceApproved",
            "ManualAttendanceRejected",
            "TaskCreated",
            "TaskUpdated",
            "TaskCompleted",
            "IssueReported",
            "IssueUpdated",
            "UserCreated",
            "UserUpdated",
            "WorkerTransfer",
            "BulkRoleAssignment",
            "BulkStatusChange",
            "FileAccess",
            "PasswordChanged",
            "PasswordReset",
            "DeviceReset"
        };

        return Ok(ApiResponse<object>.SuccessResponse(actions));
    }
}
