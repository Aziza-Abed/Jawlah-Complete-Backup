using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Tasks;

public class ApproveOverrideRequest
{
    [StringLength(1000, ErrorMessage = "ملاحظات المشرف لا يمكن أن تتجاوز 1000 حرف")]
    public string? SupervisorNotes { get; set; } // Optional notes from supervisor explaining why they approved
}
