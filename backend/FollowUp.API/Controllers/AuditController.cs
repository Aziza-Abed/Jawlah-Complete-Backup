using System.Security.Claims;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FollowUp.API.Controllers;

// UR23: Audit log viewer for administrators
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class AuditController : ControllerBase
{
    private readonly AuditLogService _auditService;
    private readonly IUserRepository _users;

    public AuditController(AuditLogService auditService, IUserRepository users)
    {
        _auditService = auditService;
        _users = users;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    private async Task<HashSet<int>> GetSupervisorWorkerIdsAsync(int supervisorId)
    {
        var workers = await _users.GetWorkersBySupervisorAsync(supervisorId);
        var ids = workers.Select(w => w.UserId).ToHashSet();
        ids.Add(supervisorId); // Supervisor can also see their own logs
        return ids;
    }

    // Get recent audit logs
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int count = 100,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null)
    {
        IEnumerable<Core.Entities.AuditLog> logs = await _auditService.GetRecentLogsAsync(count, userId, action);

        // SECURITY: Supervisors can only see logs for their workers
        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();
        if (currentRole == "Supervisor" && currentUserId.HasValue)
        {
            var allowedIds = await GetSupervisorWorkerIdsAsync(currentUserId.Value);
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
        IEnumerable<Core.Entities.AuditLog> logs = await _auditService.GetLogsByDateRangeAsync(from, to);

        // SECURITY: Supervisors can only see logs for their workers
        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();
        if (currentRole == "Supervisor" && currentUserId.HasValue)
        {
            var allowedIds = await GetSupervisorWorkerIdsAsync(currentUserId.Value);
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
