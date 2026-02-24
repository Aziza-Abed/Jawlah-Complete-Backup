using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Services;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Resolve Task ambiguity (Entity vs Threading)
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Infrastructure.Services;

// sms-based OTP service for two-factor authentication
// all roles: OTP only on first login or new device (device binding)
// once device is registered, no OTP needed for that device
public class OtpService : IOtpService
{
    private readonly FollowUpDbContext _db;
    private readonly ISmsService _sms;
    private readonly ILogger<OtpService> _logger;

    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_ATTEMPTS = 3;

    // IP-based rate limiting to prevent brute force attacks
    private static readonly ConcurrentDictionary<string, (int FailedCount, DateTime LockoutUntil)> _ipRateLimits = new();
    private const int MAX_FAILED_ATTEMPTS_PER_IP = 10;
    private const int IP_LOCKOUT_MINUTES = 15;
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public OtpService(FollowUpDbContext db, ISmsService sms, ILogger<OtpService> logger)
    {
        _db = db;
        _sms = sms;
        _logger = logger;
    }

    // determine if user needs OTP verification based on device binding
    // implements a "trust this device" security pattern
    //
    // policy (applies to all roles):
    // - first login: OTP required to register device
    // - same device: no OTP needed (device is trusted)
    // - new device: OTP required to verify identity
    public bool RequiresOtp(User user, string? deviceId)
    {
        // If no device registered yet, this is first login - require OTP to register device
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
    public async Task<string?> GenerateAndSendOtpAsync(User user, string purpose = "Login", string? deviceId = null)
    {
        try
        {
            // Check if phone number exists
            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                _logger.LogWarning("Cannot send OTP - User {UserId} has no phone number", user.UserId);
                return null;
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
                return null;
            }

            _logger.LogInformation("OTP sent to user {UserId} for {Purpose}", user.UserId, purpose);
            return sessionToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OTP for user {UserId}", user.UserId);
            return null;
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

    // store pending JWT token for session (for load-balanced environments)
    public async Task StorePendingTokenAsync(string sessionToken, string jwtToken)
    {
        var record = await _db.TwoFactorCodes
            .Where(c => c.SessionToken == sessionToken && !c.IsUsed)
            .FirstOrDefaultAsync();

        if (record != null)
        {
            record.PendingJwtToken = jwtToken;
            await _db.SaveChangesAsync();
            _logger.LogDebug("Stored pending JWT token for 2FA session");
        }
    }

    // retrieve and clear pending JWT token for session
    public async Task<string?> GetAndClearPendingTokenAsync(string sessionToken)
    {
        var record = await _db.TwoFactorCodes
            .Where(c => c.SessionToken == sessionToken)
            .FirstOrDefaultAsync();

        if (record?.PendingJwtToken == null)
        {
            _logger.LogWarning("No pending JWT token found for 2FA session");
            return null;
        }

        var token = record.PendingJwtToken;
        record.PendingJwtToken = null; // Clear after retrieval
        await _db.SaveChangesAsync();

        _logger.LogDebug("Retrieved and cleared pending JWT token for 2FA session");
        return token;
    }

    // get session info without clearing (for resend OTP)
    public async Task<(int? UserId, string? JwtToken)> GetSessionInfoAsync(string sessionToken)
    {
        var record = await _db.TwoFactorCodes
            .Where(c => c.SessionToken == sessionToken && !c.IsUsed)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return (null, null);
        }

        return (record.UserId, record.PendingJwtToken);
    }
}
