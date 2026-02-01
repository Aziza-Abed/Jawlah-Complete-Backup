using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Appeals;

/// <summary>
/// Response containing appeal details
/// </summary>
public class AppealResponse
{
    public int AppealId { get; set; }
    public AppealType AppealType { get; set; }
    public string AppealTypeName { get; set; } = string.Empty; // Friendly name
    public string EntityType { get; set; } = string.Empty; // "Task" or "Attendance"
    public int EntityId { get; set; }

    // Worker details
    public int UserId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string WorkerExplanation { get; set; } = string.Empty;

    // Location details
    public double? WorkerLatitude { get; set; }
    public double? WorkerLongitude { get; set; }
    public double? ExpectedLatitude { get; set; }
    public double? ExpectedLongitude { get; set; }
    public int? DistanceMeters { get; set; }

    // Status
    public AppealStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty; // Friendly name

    // Review details
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    // Timestamps
    public DateTime SubmittedAt { get; set; }

    // Evidence
    public string? EvidencePhotoUrl { get; set; }

    // Original rejection reason
    public string? OriginalRejectionReason { get; set; }

    // Related entity details (for display)
    public string? EntityTitle { get; set; } // Task title or attendance date
}
