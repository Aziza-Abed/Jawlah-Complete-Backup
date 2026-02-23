using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, string? Token, string? Error)> LoginAsync(string username, string password);
    Task<(bool Success, User? User, string? Error)> RegisterAsync(User user, string password);
    Task LogoutAsync(int userId);
    Task<(bool Success, string? Token, string? Error)> GenerateTokenForUserAsync(User user);
    Task<string> GenerateRefreshTokenAsync(int userId, string? deviceId, string? ipAddress);
    Task<(bool Success, string? AccessToken, string? NewRefreshToken, string? Error)> RefreshAccessTokenAsync(string refreshToken);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
