using Jawlah.Core.Enums;

namespace Jawlah.Core.DTOs.Tasks;

public class TaskResponse
{
    public int TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; } = string.Empty;
    public int? AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public TaskPriority Priority { get; set; }
    public Enums.TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LocationDescription { get; set; }
    public string? CompletionNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
