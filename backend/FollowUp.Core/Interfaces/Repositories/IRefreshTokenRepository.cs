using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Repositories;

// repository for refresh token operations
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken?> GetActiveTokenByUserIdAsync(int userId, string? deviceId = null);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId);
    Task AddAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAllUserTokensAsync(int userId);
    Task RevokeTokenAsync(string token);
    Task DeleteExpiredTokensAsync();
    Task SaveChangesAsync();
}
