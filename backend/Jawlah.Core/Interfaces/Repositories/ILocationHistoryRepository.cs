using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Repositories;

public interface ILocationHistoryRepository : IRepository<LocationHistory>
{
    Task<IEnumerable<LocationHistory>> GetUserHistoryAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<LocationHistory>> GetLatestLocationsAsync(DateTime date);
}
