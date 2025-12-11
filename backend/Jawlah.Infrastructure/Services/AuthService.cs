using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly Infrastructure.Data.JawlahDbContext _dbContext;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, Infrastructure.Data.JawlahDbContext dbContext)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<(bool Success, string? Token, string? RefreshToken, string? Error)> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);

        if (user == null)
        {
            return (false, null, null, "Invalid username or password");
        }

        if (user.Status != UserStatus.Active)
        {
            return (false, null, null, "User account is not active");
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return (false, null, null, "Invalid username or password");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var token = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.UserId);

        return (true, token, refreshToken.Token, null);
    }

    public async Task<(bool Success, User? User, string? Error)> RegisterAsync(User user, string password)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(user.Username);
        if (existingUser != null)
        {
            return (false, null, "Username already exists");
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            var existingEmail = await _userRepository.GetByEmailAsync(user.Email);
            if (existingEmail != null)
            {
                return (false, null, "Email already exists");
            }
        }

        user.PasswordHash = HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        user.Status = UserStatus.Active;

        await _userRepository.AddAsync(user);
        await _dbContext.SaveChangesAsync();

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
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null)
        {
            return (false, null, "Invalid refresh token");
        }

        if (storedToken.IsRevoked)
        {
            // Token has been revoked, possibly compromised - revoke all tokens for user
            await RevokeAllUserTokensAsync(storedToken.UserId, "Attempted reuse of revoked token");
            return (false, null, "Token has been revoked");
        }

        if (storedToken.IsExpired)
        {
            return (false, null, "Refresh token has expired");
        }

        if (storedToken.User.Status != UserStatus.Active)
        {
            return (false, null, "User account is not active");
        }

        // Revoke old token
        storedToken.RevokedAt = DateTime.UtcNow;

        // Create new tokens
        var newJwtToken = GenerateJwtToken(storedToken.User);
        var newRefreshToken = await CreateRefreshTokenAsync(storedToken.UserId);

        // Link old token to new one
        storedToken.ReplacedByToken = newRefreshToken.Token;

        await _dbContext.SaveChangesAsync();

        return (true, newJwtToken, null);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string? deviceInfo = null, string? ipAddress = null)
    {
        var refreshTokenDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return refreshToken;
    }

    private async Task RevokeAllUserTokensAsync(int userId, string reason)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task LogoutAsync(int userId)
    {
        await RevokeAllUserTokensAsync(userId, "User logout");
    }

    public async Task<(bool Success, string? Token, string? RefreshToken, string? Error)> GenerateTokenForUserAsync(User user)
    {
        if (user == null)
        {
            return (false, null, null, "User cannot be null");
        }

        if (user.Status != UserStatus.Active)
        {
            return (false, null, null, "User account is not active");
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Generate tokens
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

        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "1440");

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
