namespace FollowUp.Core.Entities;

// audit logging for critical user actions
public class AuditLog
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;  // Login, CheckIn, CheckOut, TaskUpdate, IssueReport, etc.
    public string? Details { get; set; }  // Additional info about the action
    public string? EntityType { get; set; }  // e.g. "Task", "Attendance", "Issue", "User"
    public int? EntityId { get; set; }  // ID of the affected entity
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
