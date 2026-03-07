using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Tasks;

public class CreateTaskTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int? ZoneId { get; set; }

    [Required]
    public string Frequency { get; set; } = "Daily"; // Daily, Weekly, Monthly

    [Required]
    public string Time { get; set; } = "08:00"; // HH:mm

    // Task fields — mirrors CreateTaskRequest
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskType? TaskType { get; set; }
    public bool RequiresPhotoProof { get; set; } = true;

    [Range(1, 1440)]
    public int? EstimatedDurationMinutes { get; set; }

    [MaxLength(500)]
    public string? LocationDescription { get; set; }

    // Assignment: worker OR team (not both)
    public int? DefaultAssignedToUserId { get; set; }
    public int? DefaultTeamId { get; set; }
    public bool IsTeamTask { get; set; } = false;
}
