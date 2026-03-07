using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Teams;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Teams")]
public class TeamsController : BaseApiController
{
    private readonly ITeamRepository _teams;
    private readonly IDepartmentRepository _departments;
    private readonly IUserRepository _users;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(
        ITeamRepository teams,
        IDepartmentRepository departments,
        IUserRepository users,
        ILogger<TeamsController> logger)
    {
        _teams = teams;
        _departments = departments;
        _users = users;
        _logger = logger;
    }

    // get all teams for task assignment dropdown
    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get all teams with member count")]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        // supervisors only see their department teams
        int? departmentFilter = null;
        var (isSupervisor, supervisorDeptId) = await GetSupervisorDepartmentAsync();
        if (isSupervisor)
        {
            if (supervisorDeptId.HasValue)
            {
                departmentFilter = supervisorDeptId.Value;
            }
            else
            {
                return Ok(ApiResponse<List<TeamDto>>.SuccessResponse(new List<TeamDto>()));
            }
        }

        var results = await _teams.GetAllWithMemberCountAsync(activeOnly, departmentFilter);

        var teams = results.Select(r => MapToDto(r.Team, r.MemberCount)).ToList();

        return Ok(ApiResponse<List<TeamDto>>.SuccessResponse(teams));
    }

    // get team by ID
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get team by id")]
    public async Task<IActionResult> GetById(int id)
    {
        var (team, memberCount) = await _teams.GetByIdWithMemberCountAsync(id);

        if (team == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));

        // supervisors only view their department teams
        var (isSupervisor, supervisorDeptId) = await GetSupervisorDepartmentAsync();
        if (isSupervisor && supervisorDeptId != team.DepartmentId)
        {
            return Forbid();
        }

        return Ok(ApiResponse<TeamDto>.SuccessResponse(MapToDto(team, memberCount)));
    }

    // create a new team
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "create a new team")]
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
        var department = await _departments.GetByIdAsync(request.DepartmentId);
        if (department == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("القسم المحدد غير موجود"));
        }

        // Check if code already exists
        if (await _teams.CodeExistsAsync(request.Code))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("رمز الفريق موجود مسبقاً"));
        }

        // Validate team leader if provided
        if (request.TeamLeaderId.HasValue)
        {
            var leaderError = await ValidateTeamLeaderAsync(request.TeamLeaderId.Value, request.DepartmentId);
            if (leaderError != null) return leaderError;
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

        await _teams.AddAsync(team);
        await _teams.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} created: {TeamName}", team.TeamId, team.Name);

        return Ok(ApiResponse<TeamDto>.SuccessResponse(
            MapToDto(team, department.Name, 0), "تم إنشاء الفريق بنجاح"));
    }

    // update an existing team
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "update an existing team")]
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

        var team = await _teams.GetByIdWithDepartmentAsync(id);
        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        // Check if code is being changed and if new code already exists
        if (team.Code != request.Code)
        {
            if (await _teams.CodeExistsAsync(request.Code, excludeId: id))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("رمز الفريق موجود مسبقاً"));
            }
        }

        // Validate team leader if provided
        if (request.TeamLeaderId.HasValue)
        {
            var leaderError = await ValidateTeamLeaderAsync(request.TeamLeaderId.Value, team.DepartmentId, excludeTeamId: id);
            if (leaderError != null) return leaderError;
        }

        // Check if MaxMembers is being reduced below current member count
        var currentMemberCount = await _teams.GetMemberCountAsync(id);
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

        await _teams.UpdateAsync(team);
        await _teams.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} updated: {TeamName}", team.TeamId, team.Name);

        return Ok(ApiResponse<TeamDto>.SuccessResponse(
            MapToDto(team, team.Department.Name, currentMemberCount), "تم تحديث الفريق بنجاح"));
    }

    // delete a team (only if it has no members)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "delete a team if empty")]
    public async Task<IActionResult> Delete(int id)
    {
        var team = await _teams.GetByIdAsync(id);
        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        // Check if team has members
        var memberCount = await _teams.GetMemberCountAsync(id);
        if (memberCount > 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"لا يمكن حذف الفريق. يحتوي على {memberCount} عضو"));
        }

        // Check if team has assigned tasks
        var taskCount = await _teams.GetTaskCountAsync(id);
        if (taskCount > 0)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"لا يمكن حذف الفريق. يحتوي على {taskCount} مهمة مسندة"));
        }

        await _teams.DeleteAsync(team);
        await _teams.SaveChangesAsync();

        _logger.LogInformation("Team {TeamId} deleted: {TeamName}", team.TeamId, team.Name);

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم حذف الفريق بنجاح"));
    }

    // get team members
    [HttpGet("{id}/members")]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get members of a team")]
    public async Task<IActionResult> GetTeamMembers(int id)
    {
        var team = await _teams.GetByIdAsync(id);
        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        // supervisors only view members in their department
        var (isSupervisor, supervisorDeptId) = await GetSupervisorDepartmentAsync();
        if (isSupervisor && supervisorDeptId != team.DepartmentId)
        {
            return Forbid();
        }

        var members = (await _teams.GetTeamMembersAsync(id))
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.PhoneNumber,
                u.Role,
                IsTeamLeader = u.UserId == team.TeamLeaderId
            });

        return Ok(ApiResponse<object>.SuccessResponse(members));
    }

    // add worker to team
    [HttpPost("{teamId}/members/{workerId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "add a worker to a team")]
    public async Task<IActionResult> AddMemberToTeam(int teamId, int workerId)
    {
        var team = await _teams.GetByIdAsync(teamId);
        if (team == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("الفريق غير موجود"));
        }

        var worker = await _users.GetByIdAsync(workerId);
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
        var currentMemberCount = await _teams.GetMemberCountAsync(teamId);
        if (currentMemberCount >= team.MaxMembers)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"الفريق وصل للحد الأقصى ({team.MaxMembers} أعضاء)"));
        }

        worker.TeamId = teamId;
        await _users.UpdateAsync(worker);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Worker {WorkerId} added to team {TeamId}", workerId, teamId);

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم إضافة العامل إلى الفريق بنجاح"));
    }

    // remove worker from team
    [HttpDelete("{teamId}/members/{workerId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "remove a worker from a team")]
    public async Task<IActionResult> RemoveMemberFromTeam(int teamId, int workerId)
    {
        var worker = await _users.GetByIdAsync(workerId);
        if (worker == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل غير موجود"));
        }

        if (worker.TeamId != teamId)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل ليس عضواً في هذا الفريق"));
        }

        // Clear team leader reference if this worker is the team leader
        var team = await _teams.GetByIdAsync(teamId);
        if (team != null && team.TeamLeaderId == workerId)
        {
            team.TeamLeaderId = null;
            await _teams.UpdateAsync(team);
        }

        worker.TeamId = null;
        await _users.UpdateAsync(worker);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Worker {WorkerId} removed from team {TeamId}", workerId, teamId);

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم إزالة العامل من الفريق بنجاح"));
    }

    // map team entity to DTO (with navigation properties loaded)
    private static TeamDto MapToDto(Core.Entities.Team t, int membersCount) => new()
    {
        TeamId = t.TeamId,
        DepartmentId = t.DepartmentId,
        DepartmentName = t.Department?.Name ?? string.Empty,
        Name = t.Name,
        Code = t.Code,
        Description = t.Description,
        TeamLeaderId = t.TeamLeaderId,
        TeamLeaderName = t.TeamLeader?.FullName,
        MaxMembers = t.MaxMembers,
        MembersCount = membersCount,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt
    };

    // overload for Create where department name is known but navigation isn't loaded
    private static TeamDto MapToDto(Core.Entities.Team t, string departmentName, int membersCount) => new()
    {
        TeamId = t.TeamId,
        DepartmentId = t.DepartmentId,
        DepartmentName = departmentName,
        Name = t.Name,
        Code = t.Code,
        Description = t.Description,
        TeamLeaderId = t.TeamLeaderId,
        MaxMembers = t.MaxMembers,
        MembersCount = membersCount,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt
    };

    // helper: get supervisor's department ID (null if not supervisor or no department)
    private async Task<(bool IsSupervisor, int? DepartmentId)> GetSupervisorDepartmentAsync()
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "Supervisor" || !currentUserId.HasValue)
            return (false, null);

        var supervisor = await _users.GetByIdAsync(currentUserId.Value);
        return (true, supervisor?.DepartmentId);
    }

    // helper: validate team leader (exists, same department, is Worker, not leading another team)
    private async Task<IActionResult?> ValidateTeamLeaderAsync(int teamLeaderId, int departmentId, int? excludeTeamId = null)
    {
        var teamLeader = await _users.GetByIdAsync(teamLeaderId);
        if (teamLeader == null)
            return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق المحدد غير موجود"));

        if (teamLeader.DepartmentId != departmentId)
            return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق يجب أن يكون من نفس القسم"));

        if (teamLeader.Role != Core.Enums.UserRole.Worker)
            return BadRequest(ApiResponse<object>.ErrorResponse("قائد الفريق يجب أن يكون عامل"));

        if (await _teams.IsLeaderOfAnotherTeamAsync(teamLeaderId, excludeTeamId))
            return BadRequest(ApiResponse<object>.ErrorResponse("هذا العامل قائد لفريق آخر بالفعل"));

        return null;
    }
}
