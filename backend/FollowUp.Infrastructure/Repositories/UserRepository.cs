using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(FollowUpDbContext context) : base(context)
    {
    }

    public override async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(u => u.Municipality)
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.UserId == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.Municipality)
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Municipality)
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
    {
        return await _dbSet
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .Where(u => u.Role == role)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .Where(u => u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<User?> GetUserWithZonesAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<IEnumerable<User>> GetUsersByMunicipalityAsync(int municipalityId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.MunicipalityId == municipalityId)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetWorkersBySupervisorAsync(int supervisorId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.SupervisorId == supervisorId && u.Role == UserRole.Worker)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }
}
