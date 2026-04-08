using FollowUp.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FollowUp.Infrastructure.Services;

// sms service implementation
// currently uses mock/logging for development
// can be replaced with Twilio or local SMS gateway for production
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly IConfiguration _config;
    private readonly bool _useMock;

    public SmsService(ILogger<SmsService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        // Use mock SMS in development mode
        _useMock = _config.GetValue<bool>("DeveloperMode:MockSms", true);
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        if (_useMock)
        {
            // Mock implementation - log the message
            _logger.LogInformation("[MOCK SMS] To: {Phone}, Message: {Message}", phoneNumber, message);

            // Also write to a file for easy testing
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "sms_log.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                await File.AppendAllTextAsync(logPath,
                    $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] To: {phoneNumber} | Message: {message}\n");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write SMS log file — SMS was still sent successfully");
            }

            return true;
        }

        _logger.LogWarning("SMS sending is not configured for production. Enable MockSms or configure a provider.");
        return false;
    }

    public async Task<bool> SendOtpAsync(string phoneNumber, string otpCode)
    {
        var message = $"رمز التحقق الخاص بك في نظام FollowUp هو: {otpCode}\n" +
                      $"صالح لمدة 5 دقائق. لا تشاركه مع أحد.";

        return await SendSmsAsync(phoneNumber, message);
    }
}
