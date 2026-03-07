using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

// request to send SMS OTP after initial login validation
public class SendOtpRequest
{
    // temporary session token from initial login
    [Required(ErrorMessage = "رمز الجلسة مطلوب")]
    public string SessionToken { get; set; } = string.Empty;

    // device ID for tracking (optional for web)
    public string? DeviceId { get; set; }
}

// response after sending SMS OTP
public class SendOtpResponse
{
    public bool Success { get; set; }

    // session token for OTP verification (used by forgot-password flow)
    public string? SessionToken { get; set; }

    // masked phone number for display (e.g., ****1234)
    public string MaskedPhone { get; set; } = string.Empty;

    // when the OTP expires
    public DateTime ExpiresAt { get; set; }

    // message to display to user
    public string Message { get; set; } = string.Empty;

    // seconds until user can request another OTP
    public int ResendCooldownSeconds { get; set; } = 60;

    // OTP code for demo/testing (only set when MockSms is enabled, null in production)
    public string? DemoOtpCode { get; set; }
}
