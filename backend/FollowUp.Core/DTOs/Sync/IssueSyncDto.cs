using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Sync;

public class IssueSyncDto
{
    [StringLength(100)]
    public string? ClientId { get; set; }
    public int? IssueId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public IssueType Type { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueStatus Status { get; set; }
    public int ReportedByUserId { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [StringLength(500)]
    public string? LocationDescription { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }
    public List<string> Photos { get; set; } = new();
    public DateTime ReportedAt { get; set; }
    public int SyncVersion { get; set; }

    // forwarding fields
    public int? ForwardedToDepartmentId { get; set; }
    public DateTime? ForwardedAt { get; set; }

    [StringLength(1000)]
    public string? ForwardingNotes { get; set; }
}
