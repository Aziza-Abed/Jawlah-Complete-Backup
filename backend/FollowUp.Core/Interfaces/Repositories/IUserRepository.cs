using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<User?> GetUserWithZonesAsync(int userId);
    Task<IEnumerable<User>> GetUsersByMunicipalityAsync(int municipalityId);
    Task<IEnumerable<User>> GetWorkersBySupervisorAsync(int supervisorId);
}
