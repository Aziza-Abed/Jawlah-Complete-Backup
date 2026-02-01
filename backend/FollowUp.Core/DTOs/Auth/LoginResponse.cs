namespace FollowUp.Core.DTOs.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? Error { get; set; }

    /// <summary>
    /// Whether OTP verification is required before full login
    /// True for Admin/Supervisor always, Workers on new device
    /// </summary>
    public bool RequiresOtp { get; set; } = false;

    /// <summary>
    /// Temporary session token for OTP verification flow
    /// Only set when RequiresOtp is true
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Masked phone number for OTP display (e.g., ****1234)
    /// Only set when RequiresOtp is true
    /// </summary>
    public string? MaskedPhone { get; set; }
}
