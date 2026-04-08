using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Repositories;

public class LocationHistoryRepository : Repository<LocationHistory>, ILocationHistoryRepository
{
    public LocationHistoryRepository(FollowUpDbContext context) : base(context)
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
        IQueryable<LocationHistory> baseQuery = _dbSet.AsNoTracking();

        // Apply date filter if not DateTime.MinValue
        if (date != DateTime.MinValue)
        {
            var endOfDay = date.AddDays(1);
            baseQuery = baseQuery.Where(x => x.Timestamp >= date && x.Timestamp < endOfDay);
        }

        // Single query: get latest location ID per user, then fetch with includes in one round-trip
        var latestIds = await baseQuery
            .GroupBy(x => x.UserId)
            .Select(g => g.OrderByDescending(x => x.Timestamp).First().Id)
            .ToListAsync();

        if (!latestIds.Any())
            return Enumerable.Empty<LocationHistory>();

        return await _dbSet
            .AsNoTracking()
            .Include(x => x.User)
                .ThenInclude(u => u!.AssignedZones)
                    .ThenInclude(uz => uz.Zone)
            .Where(x => latestIds.Contains(x.Id))
            .ToListAsync();
    }
}
