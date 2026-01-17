using Jawlah.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetUserTokensAsync(int userId);
    Task RevokeAllUserTokensAsync(int userId);
}
