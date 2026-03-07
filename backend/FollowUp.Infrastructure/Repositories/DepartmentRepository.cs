using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Department>> GetByMunicipalityAsync(int municipalityId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(d => d.MunicipalityId == municipalityId && d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<(Department Department, int UserCount)>> GetAllWithUserCountAsync(bool? activeOnly = null)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(d => d.IsActive);

        var results = await query
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                Department = d,
                UserCount = _context.Users.Count(u => u.DepartmentId == d.DepartmentId)
            })
            .ToListAsync();

        return results.Select(r => (r.Department, r.UserCount));
    }

    public async Task<(Department? Department, int UserCount)> GetByIdWithUserCountAsync(int id)
    {
        var result = await _dbSet
            .Where(d => d.DepartmentId == id)
            .Select(d => new
            {
                Department = d,
                UserCount = _context.Users.Count(u => u.DepartmentId == d.DepartmentId)
            })
            .OrderBy(x => x.Department.DepartmentId)
            .FirstOrDefaultAsync();

        return result == null ? (null, 0) : (result.Department, result.UserCount);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _dbSet.Where(d => d.Code == code);
        if (excludeId.HasValue)
            query = query.Where(d => d.DepartmentId != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<int> GetUserCountAsync(int departmentId)
    {
        return await _context.Users.CountAsync(u => u.DepartmentId == departmentId);
    }
}
