namespace FollowUp.Core.DTOs.Sync;

public class SyncResult
{
    public string? ClientId { get; set; }
    public int? ServerId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ConflictResolution { get; set; }
    public int? ServerVersion { get; set; }
}
