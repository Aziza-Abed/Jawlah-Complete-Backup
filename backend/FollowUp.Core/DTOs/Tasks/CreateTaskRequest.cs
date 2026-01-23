using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Tasks;

public class CreateTaskRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int AssignedToUserId { get; set; }

    public int? ZoneId { get; set; }

    [Required]
    public TaskPriority Priority { get; set; }

    // enhanced task fields
    public TaskType? TaskType { get; set; }

    public bool RequiresPhotoProof { get; set; } = true;

    [Range(1, 1440)] // 1 minute to 24 hours
    public int? EstimatedDurationMinutes { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [StringLength(500)]
    public string? LocationDescription { get; set; }
}
