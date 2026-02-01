using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Tasks;

/// <summary>
/// Request for reassigning a task to a different worker
/// </summary>
public class ReassignTaskRequest
{
    [Required(ErrorMessage = "يجب تحديد العامل الجديد")]
    public int NewAssignedToUserId { get; set; }

    [StringLength(500, ErrorMessage = "سبب إعادة التعيين طويل جداً")]
    public string? ReassignmentReason { get; set; }
}
