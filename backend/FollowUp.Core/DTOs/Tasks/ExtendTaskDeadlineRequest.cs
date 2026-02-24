using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Tasks;

// request for supervisor to extend task deadline
public class ExtendTaskDeadlineRequest
{
    [Required]
    public DateTime NewDeadline { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
