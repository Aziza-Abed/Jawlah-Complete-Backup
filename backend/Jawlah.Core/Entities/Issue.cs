using Jawlah.Core.Enums;

namespace Jawlah.Core.Entities;

public class Issue
{
    public int IssueId { get; set; }
    public int ReportedByUserId { get; set; }
    public int? ZoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueType Type { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueStatus Status { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public string? PhotoUrl { get; set; }
    public string? AdditionalPhotosJson { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public int? ResolvedByUserId { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime? SyncTime { get; set; }
    public bool IsSynced { get; set; }
    public int SyncVersion { get; set; }
    public User ReportedByUser { get; set; } = null!;
    public User? ResolvedByUser { get; set; }
    public Zone? Zone { get; set; }
}
