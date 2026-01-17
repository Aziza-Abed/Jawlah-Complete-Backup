using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Jawlah.Infrastructure.Repositories;

public class ZoneRepository : Repository<Zone>, IZoneRepository
{
    public ZoneRepository(JawlahDbContext context) : base(context)
    {
    }

    public async Task<Zone?> GetByCodeAsync(string zoneCode)
    {
        return await _dbSet
            .FirstOrDefaultAsync(z => z.ZoneCode == zoneCode);
    }

    public async Task<IEnumerable<Zone>> GetActiveZonesAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(z => z.IsActive)
            .OrderBy(z => z.ZoneName)
            .ToListAsync();
    }

    public async Task<Zone?> ValidateLocationAsync(double latitude, double longitude)
    {
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var point = geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

        var zones = await _dbSet
            .AsNoTracking()
            .Where(z => z.IsActive && z.Boundary != null)
            .ToListAsync();

        var zone = zones.FirstOrDefault(z => z.Boundary!.Contains(point));
        return zone;
    }

    public async Task<IEnumerable<Zone>> GetUserZonesAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(z => z.AssignedUsers)
            .Where(z => z.IsActive && z.AssignedUsers.Any(au => au.UserId == userId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Zone>> GetZonesWithUsersAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(z => z.AssignedUsers)
                .ThenInclude(uz => uz.User)
            .Where(z => z.IsActive)
            .OrderBy(z => z.ZoneName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Zone>> GetZonesByIdsAsync(IEnumerable<int> zoneIds)
    {
        var zoneIdsList = zoneIds.ToList();
        if (!zoneIdsList.Any())
            return Enumerable.Empty<Zone>();

        return await _dbSet
            .AsNoTracking()
            .Where(z => zoneIdsList.Contains(z.ZoneId))
            .ToListAsync();
    }

    // Municipality-specific methods
    public async Task<IEnumerable<Zone>> GetActiveZonesByMunicipalityAsync(int municipalityId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(z => z.IsActive && z.MunicipalityId == municipalityId)
            .OrderBy(z => z.ZoneName)
            .ToListAsync();
    }

    public async Task<Zone?> ValidateLocationInMunicipalityAsync(double latitude, double longitude, int municipalityId)
    {
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var point = geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

        var zones = await _dbSet
            .AsNoTracking()
            .Where(z => z.IsActive && z.MunicipalityId == municipalityId && z.Boundary != null)
            .ToListAsync();

        var zone = zones.FirstOrDefault(z => z.Boundary!.Contains(point));
        return zone;
    }

    public async Task<Zone?> GetByCodeAndMunicipalityAsync(string zoneCode, int municipalityId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(z => z.ZoneCode == zoneCode && z.MunicipalityId == municipalityId);
    }
}
