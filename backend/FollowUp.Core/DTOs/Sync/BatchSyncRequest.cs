using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Sync;

public class BatchSyncRequest<T>
{
    [Required(ErrorMessage = "device id is required")]
    [StringLength(100, ErrorMessage = "device id cannot exceed 100 characters")]
    public string DeviceId { get; set; } = string.Empty;

    [Required(ErrorMessage = "client time is required")]
    public DateTime ClientTime { get; set; }

    [Required(ErrorMessage = "items list is required")]
    [MinLength(1, ErrorMessage = "at least one item is required")]
    public List<T> Items { get; set; } = new();
}
