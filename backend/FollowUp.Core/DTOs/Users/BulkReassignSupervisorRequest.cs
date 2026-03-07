using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

public class BulkReassignSupervisorRequest
{
    [Required(ErrorMessage = "يجب تحديد العمّال")]
    [MinLength(1, ErrorMessage = "يجب اختيار عامل واحد على الأقل")]
    public List<int> WorkerIds { get; set; } = new();

    [Required(ErrorMessage = "يجب تحديد المشرف الجديد")]
    public int NewSupervisorId { get; set; }
}
