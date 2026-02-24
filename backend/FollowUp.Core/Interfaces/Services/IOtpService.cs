using FollowUp.Core.Entities;

namespace FollowUp.Core.Interfaces.Services;

// service for managing SMS-based OTP verification
public interface IOtpService
{
    // check if user requires OTP verification based on role and device
    bool RequiresOtp(User user, string? deviceId);

    // generate and send OTP to user's phone, returns session token or null if failed
    System.Threading.Tasks.Task<string?> GenerateAndSendOtpAsync(User user, string purpose = "Login", string? deviceId = null);

    // verify OTP code, returns success status, user ID, error, and remaining attempts
    System.Threading.Tasks.Task<(bool Success, int? UserId, string? Error, int RemainingAttempts)> VerifyOtpAsync(string sessionToken, string otpCode, string? ipAddress = null);

    // get masked phone number for display
    string MaskPhoneNumber(string phoneNumber);

    // store pending JWT token for session (for load-balanced environments)
    System.Threading.Tasks.Task StorePendingTokenAsync(string sessionToken, string jwtToken);

    // retrieve and clear pending JWT token for session
    System.Threading.Tasks.Task<string?> GetAndClearPendingTokenAsync(string sessionToken);

    // get session info without clearing (for resend OTP)
    System.Threading.Tasks.Task<(int? UserId, string? JwtToken)> GetSessionInfoAsync(string sessionToken);
}
