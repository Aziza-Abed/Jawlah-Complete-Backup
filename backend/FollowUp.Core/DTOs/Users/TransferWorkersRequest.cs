using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

/// <summary>
/// Request to transfer all workers from one supervisor to another
/// Used when supervisor is leaving/being deactivated and workers need reassignment
/// </summary>
public class TransferWorkersRequest
{
    /// <summary>
    /// ID of the new supervisor who will take over the workers
    /// </summary>
    [Required(ErrorMessage = "يجب تحديد المشرف الجديد")]
    public int NewSupervisorId { get; set; }

    /// <summary>
    /// Optional reason for the transfer (e.g., "Supervisor leaving company")
    /// </summary>
    [StringLength(500, ErrorMessage = "سبب النقل طويل جداً")]
    public string? TransferReason { get; set; }

    /// <summary>
    /// Optional: Notify workers about the transfer
    /// Default: true
    /// </summary>
    public bool NotifyWorkers { get; set; } = true;
}
