namespace FollowUp.Core.Entities;

// SMS-based OTP for two-factor authentication
// used for Admin/Supervisor always, Workers on new device only
public class TwoFactorCode
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // hashed 6-digit OTP code (never store plain text)
    public string CodeHash { get; set; } = string.Empty;

    // when the OTP expires (5 minutes from creation)
    public DateTime ExpiresAt { get; set; }

    // whether the OTP has been used
    public bool IsUsed { get; set; } = false;

    // when the OTP was created
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // purpose: Login, PasswordReset, DeviceChange
    public string Purpose { get; set; } = "Login";

    // number of failed verification attempts (max 3)
    public int FailedAttempts { get; set; } = 0;

    // phone number the OTP was sent to (for audit)
    public string PhoneNumber { get; set; } = string.Empty;

    // device ID that requested the OTP (for tracking)
    public string? DeviceId { get; set; }

    // session token for OTP verification lookup (GUID)
    public string SessionToken { get; set; } = string.Empty;

    // JWT token to return after successful OTP verification
    // this eliminates the need for in-memory session cache (load balancer safe)
    public string? PendingJwtToken { get; set; }
}
