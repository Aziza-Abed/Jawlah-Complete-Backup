using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Repositories;

public interface IZoneRepository : IRepository<Zone>
{
    Task<Zone?> GetByCodeAsync(string zoneCode);
    Task<IEnumerable<Zone>> GetActiveZonesAsync();
    Task<Zone?> ValidateLocationAsync(double latitude, double longitude);
    Task<IEnumerable<Zone>> GetZonesWithUsersAsync();
    Task<IEnumerable<Zone>> GetZonesByIdsAsync(IEnumerable<int> zoneIds);
}
