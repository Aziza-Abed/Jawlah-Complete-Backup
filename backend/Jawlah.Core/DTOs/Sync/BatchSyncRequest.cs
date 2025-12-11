namespace Jawlah.Core.DTOs.Sync;

public class BatchSyncRequest<T>
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime ClientTime { get; set; }
    public List<T> Items { get; set; } = new();
}
