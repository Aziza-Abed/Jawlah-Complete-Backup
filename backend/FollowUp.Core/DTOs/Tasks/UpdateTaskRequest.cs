using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Tasks;

public class UpdateTaskRequest
{
    [StringLength(200, MinimumLength = 3)]
    public string? Title { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public int? AssignedToUserId { get; set; }

    public int? ZoneId { get; set; }

    public TaskPriority? Priority { get; set; }

    public DateTime? DueDate { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [StringLength(500)]
    public string? LocationDescription { get; set; }
}
