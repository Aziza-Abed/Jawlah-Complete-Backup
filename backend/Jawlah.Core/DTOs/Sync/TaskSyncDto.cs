namespace Jawlah.Core.DTOs.Sync;

public class TaskSyncDto
{
    public string? ClientId { get; set; }
    public int TaskId { get; set; }
    public string? Title { get; set; }
    public Enums.TaskStatus Status { get; set; }
    public string? CompletionNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? ZoneId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationDescription { get; set; }
    public int SyncVersion { get; set; }
}
