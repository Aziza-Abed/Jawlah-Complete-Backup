using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Issues;

public class ForwardIssueRequest
{
    [Required(ErrorMessage = "القسم المستلم مطلوب")]
    public int DepartmentId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
