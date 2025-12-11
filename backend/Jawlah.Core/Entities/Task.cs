using Jawlah.Core.Enums;

namespace Jawlah.Core.Entities;

public class Task
{
    public int TaskId { get; set; }
    public int AssignedToUserId { get; set; }
    public int? AssignedByUserId { get; set; }
    public int? ZoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public Enums.TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public string? CompletionNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime? SyncTime { get; set; }
    public bool IsSynced { get; set; }
    public int SyncVersion { get; set; }
    public User AssignedToUser { get; set; } = null!;
    public User? AssignedByUser { get; set; }
    public Zone? Zone { get; set; }
}
