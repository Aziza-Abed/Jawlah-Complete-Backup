using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace FollowUp.Core.DTOs.Appeals;

// request to submit an appeal against an auto-rejected task or failed attendance
public class SubmitAppealRequest
{
    // type of appeal (TaskRejection or AttendanceFailure)
    [Required]
    public AppealType AppealType { get; set; }

    // ID of the rejected task or failed attendance
    [Required]
    public int EntityId { get; set; }

    // worker's explanation for the location discrepancy
    [Required]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "يجب أن يكون التبرير بين 10 و 1000 حرف")]
    public string WorkerExplanation { get; set; } = string.Empty;

    // optional evidence photo to support the appeal
    public IFormFile? EvidencePhoto { get; set; }
}
