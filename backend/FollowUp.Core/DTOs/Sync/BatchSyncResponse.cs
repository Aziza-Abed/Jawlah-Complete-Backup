namespace FollowUp.Core.DTOs.Sync;

public class BatchSyncResponse
{
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<SyncResult> Results { get; set; } = new();
}
