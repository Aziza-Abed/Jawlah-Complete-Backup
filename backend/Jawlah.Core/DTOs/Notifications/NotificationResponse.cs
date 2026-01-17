namespace Jawlah.Core.DTOs.Notifications;

public class NotificationResponse
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;  // string for mobile compatibility
    public bool IsRead { get; set; }
    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
