using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Tasks;

public class TaskTemplateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MunicipalityId { get; set; }
    public int? ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty; // HH:mm
    public bool IsActive { get; set; }

    // Task fields — mirrors CreateTaskRequest
    public TaskPriority Priority { get; set; }
    public TaskType? TaskType { get; set; }
    public bool RequiresPhotoProof { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? LocationDescription { get; set; }

    // Assignment
    public int? DefaultAssignedToUserId { get; set; }
    public string? DefaultAssignedToName { get; set; }
    public int? DefaultTeamId { get; set; }
    public bool IsTeamTask { get; set; }
}
