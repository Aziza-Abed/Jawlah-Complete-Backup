namespace FollowUp.Core.DTOs.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? Error { get; set; }

    // whether OTP verification is required before full login
    // true for Admin/Supervisor always, Workers on new device
    public bool RequiresOtp { get; set; } = false;

    // temporary session token for OTP verification flow
    // only set when RequiresOtp is true
    public string? SessionToken { get; set; }

    // masked phone number for OTP display (e.g., ****1234)
    // only set when RequiresOtp is true
    public string? MaskedPhone { get; set; }

    // refresh token for obtaining new access tokens without re-login
    public string? RefreshToken { get; set; }

    // OTP code for demo/testing (only set when MockSms is enabled, null in production)
    public string? DemoOtpCode { get; set; }
}
