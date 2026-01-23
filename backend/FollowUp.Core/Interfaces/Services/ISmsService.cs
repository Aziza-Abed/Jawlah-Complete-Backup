namespace FollowUp.Core.Interfaces.Services;

/// <summary>
/// SMS sending service interface
/// Can be implemented with Twilio, local SMS gateway, or mock for testing
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Send SMS message to a phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number in international format</param>
    /// <param name="message">Message content</param>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendSmsAsync(string phoneNumber, string message);

    /// <summary>
    /// Send OTP code via SMS
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <param name="otpCode">6-digit OTP code</param>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendOtpAsync(string phoneNumber, string otpCode);
}
