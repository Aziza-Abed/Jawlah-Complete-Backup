using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Tasks;

/// <summary>
/// Request for updating task progress (for multi-day tasks)
/// </summary>
public class UpdateTaskProgressRequest
{
    [Required]
    [Range(0, 100, ErrorMessage = "نسبة التقدم يجب أن تكون بين 0 و 100")]
    public int ProgressPercentage { get; set; }

    [StringLength(1000, ErrorMessage = "ملاحظات التقدم طويلة جداً")]
    public string? ProgressNotes { get; set; }

    // Optional: Request deadline extension
    public DateTime? ExtendedDeadline { get; set; }

    // GPS for validation (prevent fake updates from home)
    [Range(-90, 90, ErrorMessage = "خط العرض غير صالح")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "خط الطول غير صالح")]
    public double? Longitude { get; set; }
}
