using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // maximum 5 failed attempts before lockout
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public async Task<(bool Success, string? Token, User? User, string? Error)> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);

        if (user == null)
        {
            _logger.LogWarning("Login failed: user '{Username}' not found", username);
            return (false, null, null, "اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        // check if account is locked
        if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(user.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes + 1;
            return (false, null, null, $"الحساب مقفل. يرجى المحاولة بعد {remainingMinutes} دقيقة");
        }

        // Reset lockout if expired
        if (user.LockoutEndTime.HasValue && user.LockoutEndTime <= DateTime.UtcNow)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;
        }

        // user must be active
        if (user.Status != UserStatus.Active)
        {
            return (false, null, null, "حساب المستخدم غير نشط");
        }

        // check password using Identity PasswordHasher
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            // increment failed attempts
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEndTime = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
                return (false, null, null, $"تم قفل الحساب بسبب {MaxFailedAttempts} محاولات فاشلة. يرجى المحاولة بعد {LockoutMinutes} دقيقة");
            }

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            var remainingAttempts = MaxFailedAttempts - user.FailedLoginAttempts;
            return (false, null, null, $"اسم المستخدم أو كلمة المرور غير صحيحة. ({remainingAttempts} محاولات متبقية)");
        }

        // Handle SuccessRehashNeeded if necessary (optional but good practice)
        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            // Will be saved below
        }

        // Successful login - reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockoutEndTime = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return (true, token, user, null);
    }

    public async Task<(bool Success, User? User, string? Error)> RegisterAsync(User user, string password)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(user.Username);
        if (existingUser != null)
        {
            return (false, null, "اسم المستخدم موجود مسبقاً");
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            var existingEmail = await _userRepository.GetByEmailAsync(user.Email);
            if (existingEmail != null)
            {
                return (false, null, "البريد الإلكتروني موجود مسبقاً");
            }
        }

        user.PasswordHash = HashPassword(user, password);
        user.CreatedAt = DateTime.UtcNow;
        user.Status = UserStatus.Active;

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return (true, user, null);
    }

    public async Task LogoutAsync(int userId)
    {
        // Revoke all active refresh tokens for this user
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
        await _refreshTokenRepository.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Token, string? Error)> GenerateTokenForUserAsync(User user)
    {
        if (user == null)
        {
            return (false, null, "المستخدم غير موجود");
        }

        if (user.Status != UserStatus.Active)
        {
            return (false, null, "حساب المستخدم غير نشط");
        }

        // update when user last logged in
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        // make new token for user
        var token = GenerateJwtToken(user);

        return (true, token, null);
    }

    // Helper for other services if needed, but primarily used internally
    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }
    
    // Kept to satisfy interface if needed, but redirected to Identity Hasher with dummy user object if simpler not possible
    // Ideally interface should be updated, but for now we adapt
    public string HashPassword(string password)
    {
        // Warning: Using a dummy user object for hashing might affect salt if user-specific logic exists.
        // But for Identity default, it's usually fine. 
        // Better to use HashPassword(User user, string password)
        return _passwordHasher.HashPassword(new User(), password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(new User(), passwordHash, password);
        return result != PasswordVerificationResult.Failed;
    }

    // generate and store a refresh token on login
    public async Task<string> GenerateRefreshTokenAsync(int userId, string? deviceId, string? ipAddress)
    {
        // revoke any existing active tokens for this user+device
        var existing = await _refreshTokenRepository.GetActiveTokenByUserIdAsync(userId, deviceId);
        if (existing != null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(existing);
        }

        // generate cryptographically secure token
        var tokenBytes = new byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        var tokenString = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            DeviceId = deviceId,
            IpAddress = ipAddress
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return tokenString;
    }

    // exchange a valid refresh token for new access + refresh tokens
    public async Task<(bool Success, string? AccessToken, string? NewRefreshToken, string? Error)> RefreshAccessTokenAsync(string refreshToken)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (stored == null)
            return (false, null, null, "رمز التحديث غير صالح");

        if (stored.IsRevoked)
            return (false, null, null, "رمز التحديث ملغى");

        if (stored.IsExpired)
            return (false, null, null, "رمز التحديث منتهي الصلاحية");

        // get the user
        var user = await _userRepository.GetByIdAsync(stored.UserId);
        if (user == null || user.Status != UserStatus.Active)
            return (false, null, null, "المستخدم غير نشط");

        // revoke old token (token rotation)
        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(stored);

        // generate new tokens
        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.UserId, stored.DeviceId, stored.IpAddress);

        // link old token to new one for audit trail
        stored.ReplacedByToken = newRefreshToken;
        await _refreshTokenRepository.UpdateAsync(stored);
        await _refreshTokenRepository.SaveChangesAsync();

        return (true, newAccessToken, newRefreshToken, null);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("WorkerType", user.WorkerType?.ToString() ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expirationMinutes = int.TryParse(jwtSettings["ExpirationMinutes"], out var expMin) ? expMin : 1440;

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
