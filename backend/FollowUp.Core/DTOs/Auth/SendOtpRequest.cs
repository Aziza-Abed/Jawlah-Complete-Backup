using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

/// <summary>
/// Request to send SMS OTP after initial login validation
/// </summary>
public class SendOtpRequest
{
    /// <summary>
    /// Temporary session token from initial login
    /// </summary>
    [Required(ErrorMessage = "رمز الجلسة مطلوب")]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Device ID for tracking (optional for web)
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// Response after sending SMS OTP
/// </summary>
public class SendOtpResponse
{
    /// <summary>
    /// Whether OTP was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Masked phone number for display (e.g., ****1234)
    /// </summary>
    public string MaskedPhone { get; set; } = string.Empty;

    /// <summary>
    /// When the OTP expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Message to display to user
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Seconds until user can request another OTP
    /// </summary>
    public int ResendCooldownSeconds { get; set; } = 60;
}
