using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

public class UserResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public WorkerType? WorkerType { get; set; }
    public string? Department { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Supervisor assignment for workers
    public int? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
}
