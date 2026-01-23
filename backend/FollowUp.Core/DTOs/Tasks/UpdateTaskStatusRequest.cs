using System.ComponentModel.DataAnnotations;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;

namespace FollowUp.Core.DTOs.Tasks;

public class UpdateTaskStatusRequest
{
    [Required]
    public TaskStatus Status { get; set; }

    public string? CompletionNotes { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public string? PhotoUrl { get; set; }
}
