using FollowUp.Core.DTOs.Tasks;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FollowUp.API.Controllers;

[ApiController]
[Route("api/task-templates")]
[Authorize(Roles = "Admin,Manager")]
public class TaskTemplatesController : ControllerBase
{
    private readonly ITaskTemplateRepository _repository;
    private readonly IUserRepository _userRepository;

    public TaskTemplatesController(ITaskTemplateRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskTemplateDto>>> GetAll()
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        
        if (currentUser == null) return Unauthorized();

        var templates = await _repository.GetAllAsync(currentUser.MunicipalityId);

        var dtos = templates.Select(t => new TaskTemplateDto
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
        });

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<TaskTemplateDto>> Create(CreateTaskTemplateDto dto)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        
        if (currentUser == null) return Unauthorized();

        // Parse time string to TimeSpan
        if (!TimeSpan.TryParse(dto.Time, out var timeSpan))
        {
            if (!TimeSpan.TryParse(dto.Time + ":00", out timeSpan))
            {
                return BadRequest("Invalid time format. Use HH:mm");
            }
        }

        var template = new TaskTemplate
        {
            Title = dto.Title,
            Description = dto.Description,
            MunicipalityId = currentUser.MunicipalityId,
            ZoneId = dto.ZoneId == 0 ? null : dto.ZoneId,
            Frequency = dto.Frequency,
            Time = timeSpan,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(template);

        var responseDto = new TaskTemplateDto
        {
            Id = created.Id,
            Title = created.Title,
            Description = created.Description,
            MunicipalityId = created.MunicipalityId,
            ZoneId = created.ZoneId,
            ZoneName = created.Zone?.ZoneName ?? "",
            Frequency = created.Frequency,
            Time = created.Time.ToString(@"hh\:mm"),
            IsActive = created.IsActive
        };

        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, responseDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        
        if (currentUser == null) return Unauthorized();

        var template = await _repository.GetByIdAsync(id);
        if (template == null) return NotFound();

        if (template.MunicipalityId != currentUser.MunicipalityId)
            return Forbid();

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}
