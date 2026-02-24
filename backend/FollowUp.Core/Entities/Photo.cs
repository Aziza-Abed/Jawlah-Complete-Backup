using TaskEntity = FollowUp.Core.Entities.Task;

namespace FollowUp.Core.Entities;

public class Photo
{
    public int PhotoId { get; set; }

    public string PhotoUrl { get; set; } = string.Empty;

    // polymorphic relationship: Can belong to Issue OR Task
    public string EntityType { get; set; } = string.Empty; // "Issue" or "Task"
    public int EntityId { get; set; } // IssueId or TaskId

    // Explicit foreign keys (match database schema)
    public int? TaskId { get; set; }
    public int? IssueId { get; set; }

    public int OrderIndex { get; set; } // For ordering photos (0, 1, 2...)

    public long? FileSizeBytes { get; set; }

    // GPS coordinates of where the photo was captured
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime UploadedAt { get; set; }
    public int? UploadedByUserId { get; set; }

    // Dual timestamp model: when captured vs when synced
    public DateTime? EventTime { get; set; }
    public DateTime? SyncTime { get; set; }

    public DateTime CreatedAt { get; set; }

    // navigation properties
    public User? UploadedByUser { get; set; }
    public TaskEntity? Task { get; set; }
    public Issue? Issue { get; set; }
}
