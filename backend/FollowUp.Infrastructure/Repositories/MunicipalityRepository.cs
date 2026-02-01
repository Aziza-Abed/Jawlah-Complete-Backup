using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class MunicipalityRepository : Repository<Municipality>, IMunicipalityRepository
{
    public MunicipalityRepository(FollowUpDbContext context) : base(context)
    {
    }

    public async Task<Municipality?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == code);
    }

    public async Task<IEnumerable<Municipality>> GetActiveAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        return await _dbSet.AnyAsync(m => m.Code == code);
    }
}
