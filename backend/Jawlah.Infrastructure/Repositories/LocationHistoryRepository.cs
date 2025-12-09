using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Repositories;

public class LocationHistoryRepository : Repository<LocationHistory>, ILocationHistoryRepository
{
    public LocationHistoryRepository(JawlahDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LocationHistory>> GetUserHistoryAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Set<LocationHistory>()
            .Where(x => x.UserId == userId && x.Timestamp >= startDate && x.Timestamp <= endDate)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();
    }
}
