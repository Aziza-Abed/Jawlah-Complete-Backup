using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Sync;

public class TaskSyncDto
{
    public string? ClientId { get; set; }

    [Required(ErrorMessage = "task id is required")]
    [Range(1, int.MaxValue, ErrorMessage = "task id must be greater than 0")]
    public int TaskId { get; set; }

    public string? Title { get; set; }

    [Required(ErrorMessage = "status is required")]
    public Enums.TaskStatus Status { get; set; }

    public string? CompletionNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Description { get; set; }
    public Enums.TaskPriority? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? ZoneId { get; set; }

    [Range(-90, 90, ErrorMessage = "latitude must be between -90 and 90")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "longitude must be between -180 and 180")]
    public double? Longitude { get; set; }

    public string? LocationDescription { get; set; }

    [Required(ErrorMessage = "sync version is required")]
    [Range(0, int.MaxValue, ErrorMessage = "sync version must be non-negative")]
    public int SyncVersion { get; set; }
}
