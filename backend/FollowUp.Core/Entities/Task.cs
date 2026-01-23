using FollowUp.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.Entities;

public class Task
{
    public int TaskId { get; set; }

    // Municipality that this task belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    public int AssignedToUserId { get; set; }
    public int? AssignedByUserId { get; set; }
    public int? ZoneId { get; set; }

    // Team assignment for shared tasks (null for individual tasks)
    // When TeamId is set, the task is shared among all team members
    public int? TeamId { get; set; }

    /// <summary>
    /// Indicates if this is a team-shared task requiring collaboration
    /// </summary>
    public bool IsTeamTask { get; set; } = false;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public Enums.TaskStatus Status { get; set; }

    // concurrency token for optimistic locking
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // new fields for enhanced task management
    public TaskType? TaskType { get; set; }
    public bool RequiresPhotoProof { get; set; } = true;
    public int? EstimatedDurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public string? CompletionNotes { get; set; }

    // legacy photo storage (semicolon-separated urls) - kept for backward compatibility
    // use Photos collection for new uploads
    public string? PhotoUrl { get; set; }

    public DateTime EventTime { get; set; }

    // Task location verification - distance from task location when completing
    public int MaxDistanceMeters { get; set; } = 100; // Default 100m radius
    public int? CompletionDistanceMeters { get; set; } // Actual distance when completed
    public bool IsDistanceWarning { get; set; } = false; // Flagged if completed too far from location

    // Progress tracking for multi-day tasks
    public int ProgressPercentage { get; set; } = 0; // 0-100%
    public string? ProgressNotes { get; set; } // Notes about partial completion (e.g., "Rain delayed work")
    public DateTime? ExtendedDeadline { get; set; } // Extended deadline for delayed tasks
    public int? ExtendedByUserId { get; set; } // Supervisor who extended the deadline

    // Auto-rejection tracking
    public bool IsAutoRejected { get; set; } = false;
    public string? RejectionReason { get; set; } // Reason for rejection (auto or manual)
    public DateTime? RejectedAt { get; set; }
    public int? RejectedByUserId { get; set; } // null if auto-rejected, userId if manual
    public double? RejectionLatitude { get; set; } // Worker's location when auto-rejected
    public double? RejectionLongitude { get; set; }
    public int? RejectionDistanceMeters { get; set; } // Distance from task location when rejected
    public int FailedCompletionAttempts { get; set; } = 0; // Track failed completion attempts for 2-strike system

    // modern photo storage - use this for new uploads
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public DateTime? SyncTime { get; set; }
    public bool IsSynced { get; set; }
    public int SyncVersion { get; set; }
    public User AssignedToUser { get; set; } = null!;
    public User? AssignedByUser { get; set; }
    public Zone? Zone { get; set; }
    public Team? Team { get; set; }

    // helper method to get all photos (both legacy PhotoUrl and Photos collection)
    public IEnumerable<string> GetAllPhotoUrls()
    {
        var urls = new List<string>();

        // add photos from Photos collection first (preferred)
        urls.AddRange(Photos.OrderBy(p => p.OrderIndex).Select(p => p.PhotoUrl));

        // fallback to legacy PhotoUrl if Photos collection is empty
        if (urls.Count == 0 && !string.IsNullOrEmpty(PhotoUrl))
        {
            urls.AddRange(PhotoUrl.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        return urls;
    }

    // helper method to migrate legacy PhotoUrl to Photos collection
    public void MigratePhotosToCollection(int uploadedByUserId)
    {
        if (string.IsNullOrEmpty(PhotoUrl) || Photos.Any())
            return;

        var urls = PhotoUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < urls.Length; i++)
        {
            Photos.Add(new Photo
            {
                PhotoUrl = urls[i],
                EntityType = "Task",
                EntityId = TaskId,
                OrderIndex = i,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = uploadedByUserId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
