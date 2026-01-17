using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Tasks;

/// <summary>
/// Request for supervisor to extend task deadline
/// </summary>
public class ExtendTaskDeadlineRequest
{
    [Required]
    public DateTime NewDeadline { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
