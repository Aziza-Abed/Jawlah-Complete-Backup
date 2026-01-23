using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Issues;

public class IssueResponse
{
    public int IssueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueType Type { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueStatus Status { get; set; }
    public int ReportedByUserId { get; set; }
    public string? ReportedByName { get; set; }  // User name who reported
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }  // Zone name for display
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public List<string> Photos { get; set; } = new();
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }

    // Sync fields for mobile offline support
    public DateTime? SyncTime { get; set; }
    public int SyncVersion { get; set; }

    // Web dashboard uses "priority" to refer to severity - provide alias
    public string Priority => Severity.ToString();
}
