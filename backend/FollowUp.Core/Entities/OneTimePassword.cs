namespace Jawlah.Core.Entities;

/// <summary>
/// One-Time Password for Two-Factor Authentication
/// </summary>
public class OneTimePassword
{
    public int OtpId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// 6-digit OTP code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Temporary session token (before 2FA verification)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// When OTP was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When OTP expires (typically 5 minutes)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether OTP has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// When OTP was used
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Number of failed verification attempts
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// IP address that requested the OTP
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Device ID that requested the OTP
    /// </summary>
    public string? DeviceId { get; set; }
}
