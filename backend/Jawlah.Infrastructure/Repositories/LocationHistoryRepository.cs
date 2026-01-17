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
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= startDate && x.Timestamp <= endDate)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<LocationHistory>> GetLatestLocationsAsync(DateTime date)
    {
        var endOfDay = date.AddDays(1);

        // get latest location per user for the given date
        return await _dbSet
            .AsNoTracking()
            .Include(x => x.User)
                .ThenInclude(u => u!.AssignedZones)
                    .ThenInclude(uz => uz.Zone)
            .Where(x => x.Timestamp >= date && x.Timestamp < endOfDay)
            .GroupBy(x => x.UserId)
            .Select(g => g.OrderByDescending(x => x.Timestamp).First())
            .ToListAsync();
    }
}
