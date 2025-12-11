namespace Jawlah.Core.DTOs.Sync;

public class BatchSyncResponse
{
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<SyncResult> Results { get; set; } = new();
}

public class SyncResult
{
    public string? ClientId { get; set; }
    public int? ServerId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ConflictResolution { get; set; }
    public int? ServerVersion { get; set; }
}
