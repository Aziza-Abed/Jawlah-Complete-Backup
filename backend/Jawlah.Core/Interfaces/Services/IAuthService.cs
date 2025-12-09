using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Services;

public interface IAuthService
{
    System.Threading.Tasks.Task<(bool Success, string? Token, string? RefreshToken, string? Error)> LoginAsync(string username, string password);
    System.Threading.Tasks.Task<(bool Success, User? User, string? Error)> RegisterAsync(User user, string password);
    System.Threading.Tasks.Task<bool> ValidateTokenAsync(string token);
    System.Threading.Tasks.Task<(bool Success, string? Token, string? Error)> RefreshTokenAsync(string refreshToken);
    System.Threading.Tasks.Task LogoutAsync(int userId);
    System.Threading.Tasks.Task<(bool Success, string? Token, string? RefreshToken, string? Error)> GenerateTokenForUserAsync(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
