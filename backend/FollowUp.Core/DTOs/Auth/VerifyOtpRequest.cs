using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

/// <summary>
/// Request to verify SMS OTP code
/// </summary>
public class VerifyOtpRequest
{
    /// <summary>
    /// Temporary session token from initial login
    /// </summary>
    [Required(ErrorMessage = "رمز الجلسة مطلوب")]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// 6-digit OTP code from SMS
    /// </summary>
    [Required(ErrorMessage = "رمز التحقق مطلوب")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "رمز التحقق يجب أن يكون 6 أرقام")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "رمز التحقق يجب أن يحتوي على أرقام فقط")]
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>
    /// Device ID for registration (optional)
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// Response after successful OTP verification
/// </summary>
public class VerifyOtpResponse
{
    /// <summary>
    /// Whether verification was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// JWT access token (only if successful)
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// User details (only if successful)
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// Error message (only if failed)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Remaining attempts before lockout
    /// </summary>
    public int? RemainingAttempts { get; set; }
}
