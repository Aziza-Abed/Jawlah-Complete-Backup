using System.ComponentModel.DataAnnotations;
using Jawlah.Core.Enums;

namespace Jawlah.Core.DTOs.Issues;

public class ReportIssueRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public IssueType Type { get; set; }

    [Required]
    public IssueSeverity Severity { get; set; }

    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    [StringLength(500)]
    public string? LocationDescription { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }
}
