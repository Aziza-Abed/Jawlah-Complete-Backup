using Jawlah.Core.Enums;

namespace Jawlah.Core.Entities;

public class SyncLog
{
    public int SyncLogId { get; set; }
    public int UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public SyncAction Action { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime SyncTime { get; set; }
    public bool HadConflict { get; set; }
    public string? ConflictResolution { get; set; }
    public string? ConflictDetails { get; set; }
    public string? DeviceId { get; set; }
    public string? AppVersion { get; set; }
    public User User { get; set; } = null!;
}
