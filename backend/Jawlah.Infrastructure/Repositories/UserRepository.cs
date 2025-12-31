using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(JawlahDbContext context) : base(context)
    {
    }

    public override async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.UserId == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByPinAsync(string pin)
    {
        return await _dbSet
            .Include(u => u.AssignedZones)
                .ThenInclude(uz => uz.Zone)
            .FirstOrDefaultAsync(u => u.Pin == pin && u.Role == UserRole.Worker);
    }

    public async Task<bool> IsPinUniqueAsync(string pin, int? excludeUserId = null)
    {
        var query = _dbSet.Where(u => u.Pin == pin);
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.UserId != excludeUserId.Value);
        }
        return !await query.AnyAsync();
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
}
