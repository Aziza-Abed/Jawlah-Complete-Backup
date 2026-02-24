using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Appeals;

// request to approve or reject an appeal (supervisor action)
public class ReviewAppealRequest
{
    // whether to approve the appeal (true) or reject it (false)
    [Required]
    public bool Approved { get; set; }

    // supervisor's notes about the decision
    [StringLength(1000, ErrorMessage = "يجب أن تكون الملاحظات أقل من 1000 حرف")]
    public string? ReviewNotes { get; set; }
}
