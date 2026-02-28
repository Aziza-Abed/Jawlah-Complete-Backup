using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Tasks;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FollowUp.API.Controllers;

[Route("api/task-templates")]
[Authorize(Roles = "Admin,Supervisor")]
public class TaskTemplatesController : BaseApiController
{
    private readonly ITaskTemplateRepository _repository;
    private readonly IUserRepository _userRepository;

    public TaskTemplatesController(ITaskTemplateRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    [HttpGet]
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
    public async Task<IActionResult> Create(CreateTaskTemplateDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();

        var currentUser = await _userRepository.GetByIdAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

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
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(template);

        return CreatedAtAction(nameof(GetAll), new { id = created.Id },
            ApiResponse<TaskTemplateDto>.SuccessResponse(MapToDto(created)));
    }

    [HttpDelete("{id}")]
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
        IsActive = t.IsActive
    };
}
