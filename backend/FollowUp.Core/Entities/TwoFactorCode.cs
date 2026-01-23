namespace FollowUp.Core.Entities;

/// <summary>
/// SMS-based One-Time Password for Two-Factor Authentication
/// Used for Admin/Supervisor always, Workers on new device only
/// </summary>
public class TwoFactorCode
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Hashed 6-digit OTP code (never store plain text)
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// When the OTP expires (5 minutes from creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the OTP has been used
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// When the OTP was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Purpose: Login, PasswordReset, DeviceChange
    /// </summary>
    public string Purpose { get; set; } = "Login";

    /// <summary>
    /// Number of failed verification attempts (max 3)
    /// </summary>
    public int FailedAttempts { get; set; } = 0;

    /// <summary>
    /// Phone number the OTP was sent to (for audit)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Device ID that requested the OTP (for tracking)
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Session token for OTP verification lookup (GUID)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// JWT token to return after successful OTP verification
    /// This eliminates the need for in-memory session cache (load balancer safe)
    /// </summary>
    public string? PendingJwtToken { get; set; }
}
