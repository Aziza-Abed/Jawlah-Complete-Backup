using Jawlah.Core.Enums;

namespace Jawlah.Core.Entities;

public class Notification
{
    public int NotificationId { get; set; }

    // Municipality that this notification belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? FcmMessageId { get; set; }
    public string? PayloadJson { get; set; }
    public User User { get; set; } = null!;
}
