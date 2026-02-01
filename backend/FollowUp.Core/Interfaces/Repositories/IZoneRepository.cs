using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Repositories;

public interface IZoneRepository : IRepository<Zone>
{
    Task<Zone?> GetByCodeAsync(string zoneCode);
    Task<IEnumerable<Zone>> GetActiveZonesAsync();
    Task<Zone?> ValidateLocationAsync(double latitude, double longitude);
    Task<IEnumerable<Zone>> GetUserZonesAsync(int userId);
    Task<IEnumerable<Zone>> GetZonesWithUsersAsync();
    Task<IEnumerable<Zone>> GetZonesByIdsAsync(IEnumerable<int> zoneIds);

    // Municipality-specific methods
    Task<IEnumerable<Zone>> GetActiveZonesByMunicipalityAsync(int municipalityId);
    Task<Zone?> ValidateLocationInMunicipalityAsync(double latitude, double longitude, int municipalityId);
    Task<Zone?> GetByCodeAndMunicipalityAsync(string zoneCode, int municipalityId);
}
