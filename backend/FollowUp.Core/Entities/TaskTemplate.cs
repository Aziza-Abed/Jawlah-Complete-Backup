using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FollowUp.Core.Entities;

public class TaskTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int MunicipalityId { get; set; }

    public int? ZoneId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Frequency { get; set; } = "Daily"; // Daily, Weekly, Monthly

    // Stored as string "HH:mm" for simplicity in JSON/UI, or TimeSpan
    // Plan said TimeSpan, let's stick to TimeSpan but ensure it serializes nicely or use string "HH:mm" if easier for frontend. 
    // The frontend sends "HH:mm". TimeSpan in C# works fine with EF.
    public TimeSpan Time { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastGeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("MunicipalityId")]
    public virtual Municipality? Municipality { get; set; }
    
    [ForeignKey("ZoneId")]
    public virtual Zone? Zone { get; set; }
}
