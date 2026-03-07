using FollowUp.Core.Entities;

namespace FollowUp.Core.Interfaces.Services;

// service for managing SMS-based OTP verification
public interface IOtpService
{
    // check if user requires OTP verification based on role and device
    bool RequiresOtp(User user, string? deviceId);

    // generate and send OTP to user's phone
    // returns (sessionToken, otpCodeForDemo) — otpCodeForDemo is only set when MockSms is enabled
    System.Threading.Tasks.Task<(string? SessionToken, string? OtpCode)> GenerateAndSendOtpAsync(User user, string purpose = "Login", string? deviceId = null);

    // verify OTP code, returns success status, user ID, error, and remaining attempts
    System.Threading.Tasks.Task<(bool Success, int? UserId, string? Error, int RemainingAttempts)> VerifyOtpAsync(string sessionToken, string otpCode, string? ipAddress = null);

    // get masked phone number for display
    string MaskPhoneNumber(string phoneNumber);

    // get user ID for an active session (for resend OTP)
    System.Threading.Tasks.Task<int?> GetSessionUserIdAsync(string sessionToken);
}
