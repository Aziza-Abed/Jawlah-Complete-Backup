using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace FollowUp.Core.DTOs.Appeals;

/// <summary>
/// Request to submit an appeal against an auto-rejected task or failed attendance
/// </summary>
public class SubmitAppealRequest
{
    /// <summary>
    /// Type of appeal (TaskRejection or AttendanceFailure)
    /// </summary>
    [Required]
    public AppealType AppealType { get; set; }

    /// <summary>
    /// ID of the rejected task or failed attendance
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// Worker's explanation for the location discrepancy
    /// </summary>
    [Required]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "يجب أن يكون التبرير بين 10 و 1000 حرف")]
    public string WorkerExplanation { get; set; } = string.Empty;

    /// <summary>
    /// Optional evidence photo to support the appeal
    /// </summary>
    public IFormFile? EvidencePhoto { get; set; }
}
