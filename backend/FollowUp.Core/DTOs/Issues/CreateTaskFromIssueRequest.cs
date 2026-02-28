using FollowUp.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Issues;

public class CreateTaskFromIssueRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار عامل لتعيين المهمة")]
    public int AssignedToUserId { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public bool RequiresPhotoProof { get; set; } = true;

    public TaskType? TaskType { get; set; }
}
