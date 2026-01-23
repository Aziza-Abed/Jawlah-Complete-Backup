using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Appeals;

/// <summary>
/// Request to approve or reject an appeal (supervisor action)
/// </summary>
public class ReviewAppealRequest
{
    /// <summary>
    /// Whether to approve the appeal (true) or reject it (false)
    /// </summary>
    [Required]
    public bool Approved { get; set; }

    /// <summary>
    /// Supervisor's notes about the decision
    /// </summary>
    [StringLength(1000, ErrorMessage = "يجب أن تكون الملاحظات أقل من 1000 حرف")]
    public string? ReviewNotes { get; set; }
}
