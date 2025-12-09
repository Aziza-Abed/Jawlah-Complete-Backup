using Jawlah.Core.Enums;

namespace Jawlah.Core.DTOs.Sync;

public class IssueSyncDto
{
    public string? ClientId { get; set; }
    public int? IssueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueType Type { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueStatus Status { get; set; }
    public int ReportedByUserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime ReportedAt { get; set; }
    public int SyncVersion { get; set; }
}
