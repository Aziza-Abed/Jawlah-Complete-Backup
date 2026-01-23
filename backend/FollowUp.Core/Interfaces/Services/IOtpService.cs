using FollowUp.Core.Entities;

namespace FollowUp.Core.Interfaces.Services;

/// <summary>
/// Service for managing SMS-based OTP verification
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Check if user requires OTP verification based on role and device
    /// </summary>
    /// <param name="user">The user to check</param>
    /// <param name="deviceId">Current device ID</param>
    /// <returns>True if OTP is required</returns>
    bool RequiresOtp(User user, string? deviceId);

    /// <summary>
    /// Generate and send OTP to user's phone
    /// </summary>
    /// <param name="user">User to send OTP to</param>
    /// <param name="purpose">Purpose: Login, PasswordReset, DeviceChange</param>
    /// <param name="deviceId">Device requesting OTP</param>
    /// <returns>Session token for verification, or null if failed</returns>
    System.Threading.Tasks.Task<string?> GenerateAndSendOtpAsync(User user, string purpose = "Login", string? deviceId = null);

    /// <summary>
    /// Verify OTP code
    /// </summary>
    /// <param name="sessionToken">Session token from GenerateAndSendOtpAsync</param>
    /// <param name="otpCode">6-digit code entered by user</param>
    /// <returns>User ID if valid, null if invalid</returns>
    System.Threading.Tasks.Task<(bool Success, int? UserId, string? Error, int RemainingAttempts)> VerifyOtpAsync(string sessionToken, string otpCode);

    /// <summary>
    /// Get masked phone number for display
    /// </summary>
    string MaskPhoneNumber(string phoneNumber);

    /// <summary>
    /// Clean up expired OTP codes (called periodically)
    /// </summary>
    System.Threading.Tasks.Task CleanupExpiredCodesAsync();

    /// <summary>
    /// Store pending JWT token for session (for load-balanced environments)
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <param name="jwtToken">JWT token to store</param>
    System.Threading.Tasks.Task StorePendingTokenAsync(string sessionToken, string jwtToken);

    /// <summary>
    /// Retrieve and clear pending JWT token for session
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <returns>JWT token if found, null otherwise</returns>
    System.Threading.Tasks.Task<string?> GetAndClearPendingTokenAsync(string sessionToken);

    /// <summary>
    /// Get session info without clearing (for resend OTP)
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <returns>UserId and JWT token if found</returns>
    System.Threading.Tasks.Task<(int? UserId, string? JwtToken)> GetSessionInfoAsync(string sessionToken);
}
