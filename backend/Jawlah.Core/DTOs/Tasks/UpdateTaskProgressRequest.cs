using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Tasks;

/// <summary>
/// Request for updating task progress (for multi-day tasks)
/// </summary>
public class UpdateTaskProgressRequest
{
    [Required]
    [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100")]
    public int ProgressPercentage { get; set; }

    [StringLength(500)]
    public string? ProgressNotes { get; set; }

    // Optional location verification
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
