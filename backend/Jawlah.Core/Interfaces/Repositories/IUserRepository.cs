using Jawlah.Core.Entities;
using Jawlah.Core.Enums;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPinAsync(string pin);
    Task<bool> IsPinUniqueAsync(string pin, int? excludeUserId = null);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<User?> GetUserWithZonesAsync(int userId);
}
