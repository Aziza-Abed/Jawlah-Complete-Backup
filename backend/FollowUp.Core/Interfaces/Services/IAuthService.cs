using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, string? Token, string? Error)> LoginAsync(string username, string password);
    Task<(bool Success, User? User, string? Error)> RegisterAsync(User user, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task LogoutAsync(int userId);
    Task<(bool Success, string? Token, string? Error)> GenerateTokenForUserAsync(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
