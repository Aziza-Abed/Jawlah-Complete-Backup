using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FollowUp.Core.Enums;

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

    public TimeSpan Time { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastGeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Task fields (mirrors CreateTaskRequest so generated tasks are identical) ---

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public TaskType? TaskType { get; set; }

    public bool RequiresPhotoProof { get; set; } = true;

    public int? EstimatedDurationMinutes { get; set; }

    [MaxLength(500)]
    public string? LocationDescription { get; set; }

    // Assignment: either a specific worker OR a team (same rule as CreateTaskRequest)
    public int? DefaultAssignedToUserId { get; set; }

    public int? DefaultTeamId { get; set; }

    public bool IsTeamTask { get; set; } = false;

    // Navigation properties
    [ForeignKey("MunicipalityId")]
    public virtual Municipality? Municipality { get; set; }

    [ForeignKey("ZoneId")]
    public virtual Zone? Zone { get; set; }

    [ForeignKey("DefaultAssignedToUserId")]
    public virtual User? DefaultAssignedTo { get; set; }
}
