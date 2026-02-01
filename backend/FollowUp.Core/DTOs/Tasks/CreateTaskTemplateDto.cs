using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Tasks;

public class CreateTaskTemplateDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int? ZoneId { get; set; }

    [Required]
    public string Frequency { get; set; } = "Daily"; // Daily, Weekly, Monthly

    [Required]
    public string Time { get; set; } = "08:00"; // HH:mm
}
