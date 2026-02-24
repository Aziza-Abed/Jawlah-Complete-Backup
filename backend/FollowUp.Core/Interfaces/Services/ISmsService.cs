namespace FollowUp.Core.Interfaces.Services;

// SMS sending service interface
// can be implemented with Twilio, local SMS gateway, or mock for testing
public interface ISmsService
{
    // send SMS message to a phone number
    Task<bool> SendSmsAsync(string phoneNumber, string message);

    // send OTP code via SMS
    Task<bool> SendOtpAsync(string phoneNumber, string otpCode);
}
