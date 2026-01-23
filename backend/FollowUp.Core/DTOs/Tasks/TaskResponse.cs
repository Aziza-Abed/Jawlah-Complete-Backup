using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Tasks;

public class TaskResponse
{
    public int TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; } = string.Empty;
    public int? AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public TaskPriority Priority { get; set; }
    public Enums.TaskStatus Status { get; set; }

    // enhanced task fields
    public TaskType? TaskType { get; set; }
    public bool RequiresPhotoProof { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LocationDescription { get; set; }
    public string? CompletionNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public List<string> Photos { get; set; } = new();
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Task location verification
    public int MaxDistanceMeters { get; set; } = 100;
    public int? CompletionDistanceMeters { get; set; }
    public bool IsDistanceWarning { get; set; } = false;

    // Progress tracking for multi-day tasks
    public int ProgressPercentage { get; set; } = 0;
    public string? ProgressNotes { get; set; }
    public DateTime? ExtendedDeadline { get; set; }

    // Rejection tracking
    public bool IsAutoRejected { get; set; } = false;
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectionDistanceMeters { get; set; }

    // Sync fields for mobile offline support
    public DateTime? SyncTime { get; set; }
    public int SyncVersion { get; set; }
}
