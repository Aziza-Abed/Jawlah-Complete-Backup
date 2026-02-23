using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Teams;
using FollowUp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
public class TeamsController : BaseApiController
{
    private readonly FollowUpDbContext _context;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(FollowUpDbContext context, ILogger<TeamsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all teams for task assignment dropdown
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        var query = _context.Teams.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        // SECURITY: Supervisors can only see teams from their department
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole == "Supervisor" && currentUserId.HasValue)
        {
            // Get supervisor's department
            var supervisor = await _context.Users
                .Where(u => u.UserId == currentUserId.Value)
                .Select(u => new { u.DepartmentId })
                .FirstOrDefaultAsync();

            if (supervisor?.DepartmentId != null)
            {
                // Filter teams to only those in supervisor's department
                query = query.Where(t => t.DepartmentId == supervisor.DepartmentId);

                _logger.LogInformation(
                    "Supervisor {SupervisorId} querying teams (filtered to department {DepartmentId})",
                    currentUserId.Value, supervisor.DepartmentId);
            }
            else
            {
                // Supervisor has no department - return empty list
                _logger.LogWarning(
                    "Supervisor {SupervisorId} has no department assigned, returning empty team list",
                    currentUserId.Value);
                return Ok(ApiResponse<List<TeamDto>>.SuccessResponse(new List<TeamDto>()));
            }
        }

        var teams = await query
            .OrderBy(t => t.Department.Name)
            .ThenBy(t => t.Name)
            .Select(t => new TeamDto
            {
                TeamId = t.TeamId,
                DepartmentId = t.DepartmentId,
                DepartmentName = t.Department.Name,
                Name = t.Name,
                Code = t.Code,
                Description = t.Description,
                TeamLeaderId = t.TeamLeaderId,
                TeamLeaderName = t.TeamLeader != null ? t.TeamLeader.FullName : null,
                MaxMembers = t.MaxMembers,
                MembersCount = _context.Users.Count(u => u.TeamId == t.TeamId),
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<TeamDto>>.SuccessResponse(teams));
    }

    /// <summary>
    /// Get team by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetById(int id)
    {
        var team = await _context.Teams
            .Where(t => t.TeamId == id)
            .Select(t => new TeamDto
            {
                TeamId = t.TeamId,
                DepartmentId = t.DepartmentId,
                DepartmentName = t.Department.Name,
                Name = t.Name,
                Code = t.Code,
                Description = t.Description,
                TeamLeaderId = t.TeamLeaderId,
                TeamLeaderName = t.TeamLeader != null ? t.TeamLeader.FullName : null,
                MaxMembers = t.MaxMembers,
                MembersCount = _context.Users.Count(u => u.TeamId == t.TeamId),
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (team == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));

        // SECURITY: Supervisors can only view teams from their department
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole == "Supervisor" && currentUserId.HasValue)
        {
            var supervisor = await _context.Users
                .Where(u => u.UserId == currentUserId.Value)
                .Select(u => new { u.DepartmentId })
                .FirstOrDefaultAsync();

            if (supervisor?.DepartmentId != team.DepartmentId)
            {
                _logger.LogWarning(
                    "Supervisor {SupervisorId} attempted to access team {TeamId} from different department",
                    currentUserId.Value, id);
                return Forbid();
            }
        }

        return Ok(ApiResponse<TeamDto>.SuccessResponse(team));
    }

    /// <summary>
    /// Create a new team
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse(string.Join(", ", errors)));
        }

        // Check if department exists
        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("القسم المحدد غير موجود"));
        }

        // Check if code already exists
        var codeExists = await _context.Teams.AnyAsync(t => t.Code == request.Code);
        if (codeExists)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("رمز الفريق موجود مسبقاً"));
        }

        // Validate team leader if provided
        if (request.TeamLeaderId.HasValue)
        {
            var teamLeader = await _context.Users.FindAsync(request.TeamLeaderId.Value);
            if (teamLeader == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق المحدد غير موجود"));
            }

            // Ensure team leader is from the same department
            if (teamLeader.DepartmentId != request.DepartmentId)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق يجب أن يكون من نفس القسم"));
            }

            if (teamLeader.Role != Core.Enums.UserRole.Worker)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق يجب أن يكون عامل"));
            }

            // Check team leader uniqueness - a worker can only lead ONE team
            var existingLeadership = await _context.Teams
                .AnyAsync(t => t.TeamLeaderId == request.TeamLeaderId.Value);
            if (existingLeadership)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("هذا العامل قائد لفريق آخر بالفعل"));
            }
        }

        var team = new Core.Entities.Team
        {
            Name = InputSanitizer.SanitizeString(request.Name, 100),
            Code = request.Code.Trim().ToUpper(),
            Description = InputSanitizer.SanitizeString(request.Description, 500),
            DepartmentId = request.DepartmentId,
            TeamLeaderId = request.TeamLeaderId,
            MaxMembers = request.MaxMembers,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} created: {TeamName}", team.TeamId, team.Name);

        var teamDto = new TeamDto
        {
            TeamId = team.TeamId,
            DepartmentId = team.DepartmentId,
            DepartmentName = department.Name,
            Name = team.Name,
            Code = team.Code,
            Description = team.Description,
            TeamLeaderId = team.TeamLeaderId,
            MaxMembers = team.MaxMembers,
            MembersCount = 0,
            IsActive = team.IsActive,
            CreatedAt = team.CreatedAt
        };

        return Ok(ApiResponse<TeamDto>.SuccessResponse(teamDto, "تم إنشاء الفريق بنجاح"));
    }

    /// <summary>
    /// Update an existing team
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTeamRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse(string.Join(", ", errors)));
        }

        var team = await _context.Teams
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.TeamId == id);

        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        // Check if code is being changed and if new code already exists
        if (team.Code != request.Code)
        {
            var codeExists = await _context.Teams.AnyAsync(t => t.Code == request.Code && t.TeamId != id);
            if (codeExists)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("رمز الفريق موجود مسبقاً"));
            }
        }

        // Validate team leader if provided
        if (request.TeamLeaderId.HasValue)
        {
            var teamLeader = await _context.Users.FindAsync(request.TeamLeaderId.Value);
            if (teamLeader == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق المحدد غير موجود"));
            }

            // Ensure team leader is from the same department
            if (teamLeader.DepartmentId != team.DepartmentId)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق يجب أن يكون من نفس القسم"));
            }

            if (teamLeader.Role != Core.Enums.UserRole.Worker)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق يجب أن يكون عامل"));
            }

            // Check team leader uniqueness - a worker can only lead ONE team
            // Exclude current team from check (allow keeping same leader)
            var existingLeadership = await _context.Teams
                .AnyAsync(t => t.TeamLeaderId == request.TeamLeaderId.Value && t.TeamId != id);
            if (existingLeadership)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("هذا العامل قائد لفريق آخر بالفعل"));
            }
        }

        // Check if MaxMembers is being reduced below current member count
        var currentMemberCount = await _context.Users.CountAsync(u => u.TeamId == id);
        if (request.MaxMembers < currentMemberCount)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"لا يمكن تقليل الحد الأقصى للأعضاء. العدد الحالي: {currentMemberCount}"));
        }

        team.Name = InputSanitizer.SanitizeString(request.Name, 100);
        team.Code = request.Code.Trim().ToUpper();
        team.Description = InputSanitizer.SanitizeString(request.Description, 500);
        team.TeamLeaderId = request.TeamLeaderId;
        team.MaxMembers = request.MaxMembers;
        team.IsActive = request.IsActive;
        team.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} updated: {TeamName}", team.TeamId, team.Name);

        var teamDto = new TeamDto
        {
            TeamId = team.TeamId,
            DepartmentId = team.DepartmentId,
            DepartmentName = team.Department.Name,
            Name = team.Name,
            Code = team.Code,
            Description = team.Description,
            TeamLeaderId = team.TeamLeaderId,
            MaxMembers = team.MaxMembers,
            MembersCount = currentMemberCount,
            IsActive = team.IsActive,
            CreatedAt = team.CreatedAt
        };

        return Ok(ApiResponse<TeamDto>.SuccessResponse(teamDto, "تم تحديث الفريق بنجاح"));
    }

    /// <summary>
    /// Delete a team (only if it has no members)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var team = await _context.Teams.FindAsync(id);

        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        // Check if team has members
        var memberCount = await _context.Users.CountAsync(u => u.TeamId == id);
        if (memberCount > 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"لا يمكن حذف الفريق. يحتوي على {memberCount} عضو"));
        }

        // Check if team has assigned tasks
        var taskCount = await _context.Tasks.CountAsync(t => t.TeamId == id);
        if (taskCount > 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"لا يمكن حذف الفريق. يحتوي على {taskCount} مهمة مسندة"));
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} deleted: {TeamName}", team.TeamId, team.Name);

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم حذف الفريق بنجاح"));
    }

    /// <summary>
    /// Get team members
    /// </summary>
    [HttpGet("{id}/members")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetTeamMembers(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        // SECURITY: Supervisors can only view members of teams in their department
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole == "Supervisor" && currentUserId.HasValue)
        {
            var supervisor = await _context.Users
                .Where(u => u.UserId == currentUserId.Value)
                .Select(u => new { u.DepartmentId })
                .FirstOrDefaultAsync();

            if (supervisor?.DepartmentId != team.DepartmentId)
            {
                _logger.LogWarning(
                    "Supervisor {SupervisorId} attempted to view members of team {TeamId} from different department",
                    currentUserId.Value, id);
                return Forbid();
            }
        }

        var members = await _context.Users
            .Where(u => u.TeamId == id)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.PhoneNumber,
                u.Role,
                IsTeamLeader = u.UserId == team.TeamLeaderId
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(members));
    }

    /// <summary>
    /// Add worker to team
    /// </summary>
    [HttpPost("{teamId}/members/{workerId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddMemberToTeam(int teamId, int workerId)
    {
        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        var worker = await _context.Users.FindAsync(workerId);
        if (worker == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل غير موجود"));
        }

        // Validate worker is from same department
        if (worker.DepartmentId != team.DepartmentId)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل يجب أن يكون من نفس القسم"));
        }

        if (worker.Role != Core.Enums.UserRole.Worker)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن إضافة العمال فقط إلى الفرق"));
        }

        if (worker.TeamId.HasValue)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل موجود بالفعل في فريق آخر"));
        }

        // Check if team is at max capacity
        var currentMemberCount = await _context.Users.CountAsync(u => u.TeamId == teamId);
        if (currentMemberCount >= team.MaxMembers)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"الفريق وصل للحد الأقصى ({team.MaxMembers} أعضاء)"));
        }

        worker.TeamId = teamId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Worker {WorkerId} added to team {TeamId}", workerId, teamId);

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم إضافة العامل إلى الفريق بنجاح"));
    }

    /// <summary>
    /// Remove worker from team
    /// </summary>
    [HttpDelete("{teamId}/members/{workerId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveMemberFromTeam(int teamId, int workerId)
    {
        var worker = await _context.Users.FindAsync(workerId);
        if (worker == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل غير موجود"));
        }

        if (worker.TeamId != teamId)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل ليس عضواً في هذا الفريق"));
        }

        worker.TeamId = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Worker {WorkerId} removed from team {TeamId}", workerId, teamId);

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم إزالة العامل من الفريق بنجاح"));
    }
}
