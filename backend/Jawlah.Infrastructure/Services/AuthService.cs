using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
    }

    // SR1.5: Maximum 5 failed attempts before lockout
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public async Task<(bool Success, string? Token, string? RefreshToken, string? Error)> LoginAsync(string username, string password)
    {
        // get user from database
        var user = await _userRepository.GetByUsernameAsync(username);

        if (user == null)
        {
            return (false, null, null, "اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        // SR1.5: Check if account is locked
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

        // check password
        if (!VerifyPassword(password, user.PasswordHash))
        {
            // SR1.5: Increment failed attempts
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

        // Successful login - reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockoutEndTime = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.UserId);

        return (true, token, refreshToken.Token, null);
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

        user.PasswordHash = HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        user.Status = UserStatus.Active;

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return (true, user, null);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured"));

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = _configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<(bool Success, string? Token, string? Error)> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken == null)
        {
            return (false, null, "رمز التحديث غير صالح");
        }

        if (storedToken.IsRevoked)
        {
            return (false, null, "تم إلغاء الرمز");
        }

        if (storedToken.IsExpired)
        {
            return (false, null, "انتهت صلاحية رمز التحديث");
        }

        if (storedToken.User.Status != UserStatus.Active)
        {
            return (false, null, "حساب المستخدم غير نشط");
        }

        storedToken.RevokedAt = DateTime.UtcNow;

        var newJwtToken = GenerateJwtToken(storedToken.User);
        var newRefreshToken = await CreateRefreshTokenAsync(storedToken.UserId);

        await _userRepository.SaveChangesAsync();

        return (true, newJwtToken, null);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
    {
        var refreshTokenDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationDays"], out var days) ? days : 7;

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        return refreshToken;
    }

    private async Task RevokeAllUserTokensAsync(int userId)
    {
        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
        await _refreshTokenRepository.SaveChangesAsync();
    }

    public async Task LogoutAsync(int userId)
    {
        await RevokeAllUserTokensAsync(userId);
    }

    public async Task<(bool Success, string? Token, string? RefreshToken, string? Error)> GenerateTokenForUserAsync(User user)
    {
        if (user == null)
        {
            return (false, null, null, "المستخدم غير موجود");
        }

        if (user.Status != UserStatus.Active)
        {
            return (false, null, null, "حساب المستخدم غير نشط");
        }

        // update when user last logged in
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        // make new tokens for user
        var token = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.UserId);

        return (true, token, refreshToken.Token, null);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
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
