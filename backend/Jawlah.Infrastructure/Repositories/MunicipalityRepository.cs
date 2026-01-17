using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Repositories;

public class MunicipalityRepository : Repository<Municipality>, IMunicipalityRepository
{
    public MunicipalityRepository(JawlahDbContext context) : base(context)
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
