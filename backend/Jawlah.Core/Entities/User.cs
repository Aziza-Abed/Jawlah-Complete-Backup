using Jawlah.Core.Enums;

namespace Jawlah.Core.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Pin { get; set; } // 4-digit PIN for workers (unique identifier + auth)
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public WorkerType? WorkerType { get; set; }
    public string? Department { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? FcmToken { get; set; } // Firebase Cloud Messaging token for push notifications
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
    public ICollection<Issue> ReportedIssues { get; set; } = new List<Issue>();
    public ICollection<UserZone> AssignedZones { get; set; } = new List<UserZone>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
