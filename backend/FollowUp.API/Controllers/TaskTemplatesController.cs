using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Tasks;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/task-templates")]
[Authorize(Roles = "Admin,Supervisor")]
[Tags("Task Templates")]
public class TaskTemplatesController : BaseApiController
{
    private readonly ITaskTemplateRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<TaskTemplatesController> _logger;

    public TaskTemplatesController(ITaskTemplateRepository repository, IUserRepository userRepository, ILogger<TaskTemplatesController> logger)
    {
        _repository = repository;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "get all task templates")]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();

        var currentUser = await _userRepository.GetByIdAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        var templates = await _repository.GetAllAsync(currentUser.MunicipalityId);

        return Ok(ApiResponse<IEnumerable<TaskTemplateDto>>.SuccessResponse(
            templates.Select(MapToDto)));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "create a new task template")]
    public async Task<IActionResult> Create(CreateTaskTemplateDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();

        var currentUser = await _userRepository.GetByIdAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        var allowedFrequencies = new[] { "Daily", "Weekly", "Monthly" };
        if (!allowedFrequencies.Contains(dto.Frequency, StringComparer.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.ErrorResponse("التكرار غير صالح. القيم المسموحة: Daily, Weekly, Monthly"));

        // Parse time string to TimeSpan
        if (!TimeSpan.TryParse(dto.Time, out var timeSpan))
        {
            if (!TimeSpan.TryParse(dto.Time + ":00", out timeSpan))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("صيغة الوقت غير صالحة. استخدم HH:mm"));
            }
        }

        var template = new TaskTemplate
        {
            Title = InputSanitizer.SanitizeString(dto.Title, 200),
            Description = InputSanitizer.SanitizeString(dto.Description, 2000),
            MunicipalityId = currentUser.MunicipalityId,
            ZoneId = dto.ZoneId == 0 ? null : dto.ZoneId,
            Frequency = dto.Frequency,
            Time = timeSpan,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Priority = dto.Priority,
            TaskType = dto.TaskType,
            RequiresPhotoProof = dto.RequiresPhotoProof,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            LocationDescription = InputSanitizer.SanitizeString(dto.LocationDescription ?? "", 500),
            DefaultAssignedToUserId = dto.IsTeamTask ? null : dto.DefaultAssignedToUserId,
            DefaultTeamId = dto.IsTeamTask ? dto.DefaultTeamId : null,
            IsTeamTask = dto.IsTeamTask,
        };

        var created = await _repository.AddAsync(template);

        return CreatedAtAction(nameof(GetAll), new { id = created.Id },
            ApiResponse<TaskTemplateDto>.SuccessResponse(MapToDto(created)));
    }

    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "update a task template")]
    public async Task<IActionResult> Update(int id, CreateTaskTemplateDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();

        var currentUser = await _userRepository.GetByIdAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        var template = await _repository.GetByIdAsync(id);
        if (template == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القالب غير موجود"));

        if (template.MunicipalityId != currentUser.MunicipalityId)
            return Forbid();

        var allowedFrequencies = new[] { "Daily", "Weekly", "Monthly" };
        if (!allowedFrequencies.Contains(dto.Frequency, StringComparer.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.ErrorResponse("التكرار غير صالح. القيم المسموحة: Daily, Weekly, Monthly"));

        if (!TimeSpan.TryParse(dto.Time, out var timeSpan))
        {
            if (!TimeSpan.TryParse(dto.Time + ":00", out timeSpan))
                return BadRequest(ApiResponse<object>.ErrorResponse("صيغة الوقت غير صالحة. استخدم HH:mm"));
        }

        template.Title = InputSanitizer.SanitizeString(dto.Title, 200);
        template.Description = InputSanitizer.SanitizeString(dto.Description, 2000);
        template.ZoneId = dto.ZoneId == 0 ? null : dto.ZoneId;
        template.Frequency = dto.Frequency;
        template.Time = timeSpan;
        template.Priority = dto.Priority;
        template.TaskType = dto.TaskType;
        template.RequiresPhotoProof = dto.RequiresPhotoProof;
        template.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
        template.LocationDescription = InputSanitizer.SanitizeString(dto.LocationDescription ?? "", 500);
        template.DefaultAssignedToUserId = dto.IsTeamTask ? null : dto.DefaultAssignedToUserId;
        template.DefaultTeamId = dto.IsTeamTask ? dto.DefaultTeamId : null;
        template.IsTeamTask = dto.IsTeamTask;

        await _repository.UpdateAsync(template);

        return Ok(ApiResponse<TaskTemplateDto>.SuccessResponse(MapToDto(template)));
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "delete a task template")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();

        var currentUser = await _userRepository.GetByIdAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        var template = await _repository.GetByIdAsync(id);
        if (template == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القالب غير موجود"));

        if (template.MunicipalityId != currentUser.MunicipalityId)
            return Forbid();

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    private static TaskTemplateDto MapToDto(TaskTemplate t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        MunicipalityId = t.MunicipalityId,
        ZoneId = t.ZoneId,
        ZoneName = t.Zone?.ZoneName ?? "",
        Frequency = t.Frequency,
        Time = t.Time.ToString(@"hh\:mm"),
        IsActive = t.IsActive,
        Priority = t.Priority,
        TaskType = t.TaskType,
        RequiresPhotoProof = t.RequiresPhotoProof,
        EstimatedDurationMinutes = t.EstimatedDurationMinutes,
        LocationDescription = t.LocationDescription,
        DefaultAssignedToUserId = t.DefaultAssignedToUserId,
        DefaultAssignedToName = t.DefaultAssignedTo?.FullName,
        DefaultTeamId = t.DefaultTeamId,
        IsTeamTask = t.IsTeamTask,
    };
}
