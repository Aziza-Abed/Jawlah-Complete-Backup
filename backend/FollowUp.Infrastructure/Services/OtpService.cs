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

/// <summary>
/// SMS-based OTP service for Two-Factor Authentication
/// Policy (Updated):
/// - ALL roles: OTP only on FIRST login or NEW device (device binding)
/// - Once device is registered, no OTP needed for that device
/// </summary>
public class OtpService : IOtpService
{
    private readonly FollowUpDbContext _db;
    private readonly ISmsService _sms;
    private readonly ILogger<OtpService> _logger;

    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_ATTEMPTS = 3;

    public OtpService(FollowUpDbContext db, ISmsService sms, ILogger<OtpService> logger)
    {
        _db = db;
        _sms = sms;
        _logger = logger;
    }

    /// <summary>
    /// Determine if user needs OTP verification
    /// All users (Admin, Supervisor, Worker) use device binding:
    /// - First login: OTP required to register device
    /// - Same device: No OTP needed
    /// - New device: OTP required
    /// </summary>
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

    /// <summary>
    /// Generate OTP, store hash, and send via SMS
    /// </summary>
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

    /// <summary>
    /// Verify OTP code entered by user
    /// </summary>
    public async Task<(bool Success, int? UserId, string? Error, int RemainingAttempts)> VerifyOtpAsync(string sessionToken, string otpCode)
    {
        try
        {
            // Find the OTP record by session token (using dedicated field)
            var twoFactorCode = await _db.TwoFactorCodes
                .Include(c => c.User)
                .Where(c => c.SessionToken == sessionToken && !c.IsUsed)
                .FirstOrDefaultAsync();

            if (twoFactorCode == null)
            {
                _logger.LogWarning("OTP verification failed - invalid session token");
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

            // Verify the code
            var inputHash = HashOtpCode(otpCode);
            if (twoFactorCode.CodeHash != inputHash)
            {
                twoFactorCode.FailedAttempts++;
                await _db.SaveChangesAsync();

                var remaining = MAX_ATTEMPTS - twoFactorCode.FailedAttempts;
                _logger.LogWarning("OTP verification failed - wrong code for user {UserId}. Attempts remaining: {Remaining}",
                    twoFactorCode.UserId, remaining);

                return (false, null, $"رمز التحقق غير صحيح. المحاولات المتبقية: {remaining}", remaining);
            }

            // Success! Mark as used
            twoFactorCode.IsUsed = true;
            await _db.SaveChangesAsync();

            _logger.LogInformation("OTP verified successfully for user {UserId}", twoFactorCode.UserId);
            return (true, twoFactorCode.UserId, null, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP");
            return (false, null, "حدث خطأ أثناء التحقق. يرجى المحاولة مرة أخرى", 0);
        }
    }

    /// <summary>
    /// Mask phone number for display (e.g., ****1234)
    /// </summary>
    public string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        // Show last 4 digits
        var lastFour = phoneNumber[^4..];
        return $"****{lastFour}";
    }

    /// <summary>
    /// Clean up expired OTP codes
    /// </summary>
    public async Task CleanupExpiredCodesAsync()
    {
        var expiredCodes = await _db.TwoFactorCodes
            .Where(c => c.ExpiresAt < DateTime.UtcNow || c.IsUsed)
            .Where(c => c.CreatedAt < DateTime.UtcNow.AddDays(-1)) // Keep for 1 day for audit
            .ToListAsync();

        if (expiredCodes.Any())
        {
            _db.TwoFactorCodes.RemoveRange(expiredCodes);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired OTP codes", expiredCodes.Count);
        }
    }

    /// <summary>
    /// Generate random 6-digit OTP
    /// </summary>
    private string GenerateOtpCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return number.ToString("D6"); // Pad with zeros to ensure 6 digits
    }

    /// <summary>
    /// Hash OTP code for secure storage
    /// </summary>
    private string HashOtpCode(string otpCode)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(otpCode);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Store pending JWT token for session (for load-balanced environments)
    /// </summary>
    public async Task StorePendingTokenAsync(string sessionToken, string jwtToken)
    {
        var record = await _db.TwoFactorCodes
            .Where(c => c.SessionToken == sessionToken && !c.IsUsed)
            .FirstOrDefaultAsync();

        if (record != null)
        {
            record.PendingJwtToken = jwtToken;
            await _db.SaveChangesAsync();
            _logger.LogDebug("Stored pending JWT token for session {SessionToken}", sessionToken);
        }
    }

    /// <summary>
    /// Retrieve and clear pending JWT token for session
    /// </summary>
    public async Task<string?> GetAndClearPendingTokenAsync(string sessionToken)
    {
        var record = await _db.TwoFactorCodes
            .Where(c => c.SessionToken == sessionToken)
            .FirstOrDefaultAsync();

        if (record?.PendingJwtToken == null)
        {
            _logger.LogWarning("No pending JWT token found for session {SessionToken}", sessionToken);
            return null;
        }

        var token = record.PendingJwtToken;
        record.PendingJwtToken = null; // Clear after retrieval
        await _db.SaveChangesAsync();

        _logger.LogDebug("Retrieved and cleared pending JWT token for session {SessionToken}", sessionToken);
        return token;
    }

    /// <summary>
    /// Get session info without clearing (for resend OTP)
    /// </summary>
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
