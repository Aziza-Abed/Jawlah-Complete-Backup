using System.ComponentModel.DataAnnotations;
using Jawlah.Core.Enums;

namespace Jawlah.Core.DTOs.Issues;

public class UpdateIssueStatusRequest
{
    [Required]
    public IssueStatus Status { get; set; }

    [StringLength(2000)]
    public string? ResolutionNotes { get; set; }
}
