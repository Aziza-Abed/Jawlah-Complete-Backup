using FollowUp.Core.Enums;

namespace FollowUp.Core.Entities;

/// <summary>
/// Represents a worker's appeal against an auto-rejected task or failed attendance check-in
/// </summary>
public class Appeal
{
    public int AppealId { get; set; }

    // Municipality for multi-tenancy
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    // What is being appealed
    public AppealType AppealType { get; set; }
    public string EntityType { get; set; } = string.Empty; // "Task" or "Attendance"
    public int EntityId { get; set; } // TaskId or AttendanceId

    // Worker who submitted the appeal
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Worker's explanation
    public string WorkerExplanation { get; set; } = string.Empty;

    // Location details
    public double? WorkerLatitude { get; set; } // Where worker actually was
    public double? WorkerLongitude { get; set; }
    public double? ExpectedLatitude { get; set; } // Where worker should have been
    public double? ExpectedLongitude { get; set; }
    public int? DistanceMeters { get; set; } // Distance between actual and expected

    // Appeal status
    public AppealStatus Status { get; set; } = AppealStatus.Pending;

    // Review details
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; } // Supervisor's response

    // Timestamps
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Optional evidence photo
    public string? EvidencePhotoUrl { get; set; }

    // Original rejection reason
    public string? OriginalRejectionReason { get; set; }

    // Sync fields for offline support
    public bool IsSynced { get; set; } = true;
    public int SyncVersion { get; set; } = 1;
}
