using FollowUp.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.Entities;

public class Issue
{
    public int IssueId { get; set; }

    // Municipality that this issue belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    public int ReportedByUserId { get; set; }
    public int? ZoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueType Type { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueStatus Status { get; set; }

    // concurrency token for optimistic locking
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationDescription { get; set; }

    // legacy photo storage (semicolon-separated urls) - kept for backward compatibility
    // use Photos collection for new uploads
    public string? PhotoUrl { get; set; }

    public DateTime ReportedAt { get; set; }

    // modern photo storage - use this for new uploads
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime? SyncTime { get; set; }
    public bool IsSynced { get; set; }
    public int SyncVersion { get; set; }

    // issue forwarding to municipal departments
    public int? ForwardedToDepartmentId { get; set; }
    public DateTime? ForwardedAt { get; set; }
    public string? ForwardingNotes { get; set; }
    public int? ForwardedByUserId { get; set; }

    // Client-generated UUID for idempotent sync (prevents duplicate issues on retry)
    public string? ClientId { get; set; }
    public User ReportedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
    public Zone? Zone { get; set; }
    public Department? ForwardedToDepartment { get; set; }
}
