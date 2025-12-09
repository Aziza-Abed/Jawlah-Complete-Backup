namespace Jawlah.Core.DTOs.Sync;

public class SyncChangesResponse
{
    public DateTime LastSyncTime { get; set; }
    public DateTime CurrentServerTime { get; set; }
    public List<TaskSyncDto> Tasks { get; set; } = new();
    public List<IssueSyncDto> Issues { get; set; } = new();
}
