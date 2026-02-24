using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

// request to verify SMS OTP code
public class VerifyOtpRequest
{
    // temporary session token from initial login
    [Required(ErrorMessage = "رمز الجلسة مطلوب")]
    public string SessionToken { get; set; } = string.Empty;

    // 6-digit OTP code from SMS
    [Required(ErrorMessage = "رمز التحقق مطلوب")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "رمز التحقق يجب أن يكون 6 أرقام")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "رمز التحقق يجب أن يحتوي على أرقام فقط")]
    public string OtpCode { get; set; } = string.Empty;

    // device ID for registration (optional)
    public string? DeviceId { get; set; }
}

// response after successful OTP verification
public class VerifyOtpResponse
{
    public bool Success { get; set; }

    // JWT access token (only if successful)
    public string? Token { get; set; }

    // when the token expires
    public DateTime? ExpiresAt { get; set; }

    // user details (only if successful)
    public UserDto? User { get; set; }

    // error message (only if failed)
    public string? Error { get; set; }

    // remaining attempts before lockout
    public int? RemainingAttempts { get; set; }

    // refresh token for obtaining new access tokens
    public string? RefreshToken { get; set; }
}
