using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

// request to transfer all workers from one supervisor to another
// used when supervisor is leaving/being deactivated and workers need reassignment
public class TransferWorkersRequest
{
    // ID of the new supervisor who will take over the workers
    [Required(ErrorMessage = "يجب تحديد المشرف الجديد")]
    public int NewSupervisorId { get; set; }

    // optional reason for the transfer
    [StringLength(500, ErrorMessage = "سبب النقل طويل جداً")]
    public string? TransferReason { get; set; }

    // notify workers about the transfer (default: true)
    public bool NotifyWorkers { get; set; } = true;
}
