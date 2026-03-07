using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Services;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Resolve Task ambiguity (Entity vs Threading)
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Infrastructure.Services;

// OTP service - sends SMS codes for login verification
// OTP is only needed on first login or when switching devices
public class OtpService : IOtpService
{
    private readonly FollowUpDbContext _db;
    private readonly ISmsService _sms;
    private readonly ILogger<OtpService> _logger;
    private readonly bool _isMockSms;

    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_ATTEMPTS = 3;

    // rate limiting by IP
    private static readonly ConcurrentDictionary<string, (int FailedCount, DateTime LockoutUntil)> _ipRateLimits = new();
    private const int MAX_FAILED_ATTEMPTS_PER_IP = 10;
    private const int IP_LOCKOUT_MINUTES = 15;
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public OtpService(FollowUpDbContext db, ISmsService sms, ILogger<OtpService> logger, IConfiguration config)
    {
        _db = db;
        _sms = sms;
        _logger = logger;
        _isMockSms = config.GetValue<bool>("DeveloperMode:MockSms", true);
    }

    // check if this user needs OTP (first login or new device)
    public bool RequiresOtp(User user, string? deviceId)
    {
        // first login - no device registered yet
        if (string.IsNullOrEmpty(user.RegisteredDeviceId))
        {
            _logger.LogInformation("OTP required for {Role} user {UserId} - first device registration", user.Role, user.UserId);
            return true;
        }

        // If different device, require OTP
        if (!string.IsNullOrEmpty(deviceId) && user.RegisteredDeviceId != deviceId)
        {
            _logger.LogInformation("OTP required for {Role} user {UserId} - new device detected (registered: {Registered}, current: {Current})",
                user.Role, user.UserId, user.RegisteredDeviceId, deviceId);
            return true;
        }

        // Same device - no OTP needed
        _logger.LogDebug("No OTP required for {Role} user {UserId} - same device", user.Role, user.UserId);
        return false;
    }

    // generate OTP, store hash, and send via SMS
    public async Task<(string? SessionToken, string? OtpCode)> GenerateAndSendOtpAsync(User user, string purpose = "Login", string? deviceId = null)
    {
        try
        {
            // Check if phone number exists
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                _logger.LogWarning("Cannot send OTP - User {UserId} has no phone number", user.UserId);
                return (null, null);
            }

            // Invalidate any existing unused OTPs for this user
            var existingCodes = await _db.TwoFactorCodes
                .Where(c => c.UserId == user.UserId && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var code in existingCodes)
            {
                code.IsUsed = true; // Mark as used to invalidate
            }

            // Generate 6-digit OTP
            var otpCode = GenerateOtpCode();

            // Create session token (random GUID)
            var sessionToken = Guid.NewGuid().ToString("N");

            // Hash the OTP code before storing
            var codeHash = HashOtpCode(otpCode);

            // Create and store TwoFactorCode record
            var twoFactorCode = new TwoFactorCode
            {
                UserId = user.UserId,
                CodeHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                Purpose = purpose,
                FailedAttempts = 0,
                PhoneNumber = user.PhoneNumber,
                DeviceId = deviceId,
                SessionToken = sessionToken  // Store session token in dedicated field
            };

            _db.TwoFactorCodes.Add(twoFactorCode);
            await _db.SaveChangesAsync();

            // Send OTP via SMS
            var smsSent = await _sms.SendOtpAsync(user.PhoneNumber, otpCode);
            if (!smsSent)
            {
                _logger.LogError("Failed to send OTP SMS to user {UserId}", user.UserId);
                return (null, null);
            }

            _logger.LogInformation("OTP sent to user {UserId} for {Purpose}", user.UserId, purpose);
            // Return OTP code only in mock SMS mode (for demo/testing)
            return (sessionToken, _isMockSms ? otpCode : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OTP for user {UserId}", user.UserId);
            return (null, null);
        }
    }

    // verify OTP code entered by user
    public async Task<(bool Success, int? UserId, string? Error, int RemainingAttempts)> VerifyOtpAsync(string sessionToken, string otpCode, string? ipAddress = null)
    {
        try
        {
            // Check IP-based rate limiting first
            if (IsIpRateLimited(ipAddress, out int remainingMinutes))
            {
                _logger.LogWarning("OTP verification blocked - IP {IP} is rate limited for {Minutes} more minutes", ipAddress, remainingMinutes);
                return (false, null, $"تم تجاوز الحد الأقصى للمحاولات. يرجى المحاولة بعد {remainingMinutes} دقيقة", 0);
            }

            // Find the OTP record by session token (using dedicated field)
            var twoFactorCode = await _db.TwoFactorCodes
                .Include(c => c.User)
                .Where(c => c.SessionToken == sessionToken && !c.IsUsed)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (twoFactorCode == null)
            {
                RecordFailedIpAttempt(ipAddress);
                _logger.LogWarning("OTP verification failed - invalid session token from IP {IP}", ipAddress);
                return (false, null, "رمز الجلسة غير صالح أو منتهي الصلاحية", 0);
            }

            // Check if expired
            if (twoFactorCode.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("OTP verification failed - code expired for user {UserId}", twoFactorCode.UserId);
                twoFactorCode.IsUsed = true;
                await _db.SaveChangesAsync();
                return (false, null, "انتهت صلاحية رمز التحقق. يرجى طلب رمز جديد", 0);
            }

            // Check max attempts
            if (twoFactorCode.FailedAttempts >= MAX_ATTEMPTS)
            {
                _logger.LogWarning("OTP verification failed - max attempts exceeded for user {UserId}", twoFactorCode.UserId);
                twoFactorCode.IsUsed = true;
                await _db.SaveChangesAsync();
                return (false, null, "تم تجاوز الحد الأقصى للمحاولات. يرجى طلب رمز جديد", 0);
            }

            // Verify the OTP code
            var inputHash = HashOtpCode(otpCode);
            if (twoFactorCode.CodeHash != inputHash)
            {
                twoFactorCode.FailedAttempts++;
                await _db.SaveChangesAsync();

                // Record failed attempt for IP rate limiting
                RecordFailedIpAttempt(ipAddress);

                var remaining = MAX_ATTEMPTS - twoFactorCode.FailedAttempts;
                _logger.LogWarning("OTP verification failed - wrong code for user {UserId} from IP {IP}. Attempts remaining: {Remaining}",
                    twoFactorCode.UserId, ipAddress, remaining);

                return (false, null, $"رمز التحقق غير صحيح. المحاولات المتبقية: {remaining}", remaining);
            }

            // Success! Mark as used and reset IP rate limit
            twoFactorCode.IsUsed = true;
            await _db.SaveChangesAsync();

            // Reset IP-based rate limit on successful verification
            ResetIpAttempts(ipAddress);

            _logger.LogInformation("OTP verified successfully for user {UserId} from IP {IP}", twoFactorCode.UserId, ipAddress);
            return (true, twoFactorCode.UserId, null, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP");
            return (false, null, "حدث خطأ أثناء التحقق. يرجى المحاولة مرة أخرى", 0);
        }
    }

    // mask phone number for display (e.g., ****1234)
    public string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        // Show last 4 digits
        var lastFour = phoneNumber[^4..];
        return $"****{lastFour}";
    }

    // check if IP is rate limited for OTP verification
    private bool IsIpRateLimited(string? ipAddress, out int remainingMinutes)
    {
        remainingMinutes = 0;

        if (string.IsNullOrEmpty(ipAddress))
            return false;

        // Periodic cleanup (every 30 minutes)
        if ((DateTime.UtcNow - _lastCleanup).TotalMinutes > 30)
        {
            CleanupExpiredIpLockouts();
            _lastCleanup = DateTime.UtcNow;
        }

        if (_ipRateLimits.TryGetValue(ipAddress, out var limit))
        {
            if (DateTime.UtcNow < limit.LockoutUntil)
            {
                remainingMinutes = (int)Math.Ceiling((limit.LockoutUntil - DateTime.UtcNow).TotalMinutes);
                return true;
            }
            // Lockout expired, remove it
            _ipRateLimits.TryRemove(ipAddress, out _);
        }
        return false;
    }

    // record failed OTP attempt from IP address
    private void RecordFailedIpAttempt(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return;

        _ipRateLimits.AddOrUpdate(
            ipAddress,
            _ => (1, DateTime.MinValue),
            (_, current) =>
            {
                var newCount = current.FailedCount + 1;
                return newCount >= MAX_FAILED_ATTEMPTS_PER_IP
                    ? (newCount, DateTime.UtcNow.AddMinutes(IP_LOCKOUT_MINUTES))
                    : (newCount, current.LockoutUntil);
            });
    }

    // reset failed attempts after successful verification
    private void ResetIpAttempts(string? ipAddress)
    {
        if (!string.IsNullOrEmpty(ipAddress))
        {
            _ipRateLimits.TryRemove(ipAddress, out _);
        }
    }

    // clean up expired IP lockouts to prevent memory leak
    private void CleanupExpiredIpLockouts()
    {
        var expiredKeys = _ipRateLimits
            .Where(kvp => kvp.Value.LockoutUntil != DateTime.MinValue && kvp.Value.LockoutUntil < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _ipRateLimits.TryRemove(key, out _);
        }

        _logger.LogDebug("Cleaned up {Count} expired IP lockouts", expiredKeys.Count);
    }

    // generate random 6-digit OTP
    private string GenerateOtpCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return number.ToString("D6"); // Pad with zeros to ensure 6 digits
    }

    // hash OTP code for secure storage
    private string HashOtpCode(string otpCode)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(otpCode);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    // get user ID for an active session (for resend OTP)
    public async Task<int?> GetSessionUserIdAsync(string sessionToken)
    {
        var record = await _db.TwoFactorCodes
            .Where(c => c.SessionToken == sessionToken && !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => (int?)c.UserId)
            .FirstOrDefaultAsync();

        return record;
    }
}
