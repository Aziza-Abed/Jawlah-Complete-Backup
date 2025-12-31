using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    System.Threading.Tasks.Task<RefreshToken?> GetByTokenAsync(string token);
    System.Threading.Tasks.Task<IEnumerable<RefreshToken>> GetUserTokensAsync(int userId);
    System.Threading.Tasks.Task RevokeAllUserTokensAsync(int userId);
}
