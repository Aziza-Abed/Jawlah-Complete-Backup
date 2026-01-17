using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Services;

// this service handles all the gis stuff for municipalities
// shapefiles/geojson must be in wgs84 format same as gps
public class GisService : IGisService
{
    private readonly IZoneRepository _zoneRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILogger<GisService> _logger;
    private readonly GeometryFactory _geometryFactory;

    public GisService(
        IZoneRepository zoneRepository,
        IMunicipalityRepository municipalityRepository,
        ILogger<GisService> logger)
    {
        _zoneRepository = zoneRepository;
        _municipalityRepository = municipalityRepository;
        _logger = logger;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    public async Task<Zone?> ValidateLocationAsync(double latitude, double longitude, int? userId = null, int? municipalityId = null, double? accuracy = null)
    {
        // first check if coords are valid
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

        if (latitude == 0 && longitude == 0)
        {
            _logger.LogWarning("Received null island coordinates (0, 0) - likely GPS error");
            throw new ArgumentException("Invalid GPS coordinates received (0, 0). Please ensure GPS is enabled.");
        }

        // Get municipality for accuracy check and bounds validation
        Municipality? municipality = null;
        if (municipalityId.HasValue)
        {
            municipality = await _municipalityRepository.GetByIdAsync(municipalityId.Value);
        }

        // check gps accuracy
        var maxAccuracy = municipality?.MaxAcceptableAccuracyMeters ?? Core.Constants.GeofencingConstants.DefaultMaxAcceptableAccuracyMeters;
        if (accuracy.HasValue && accuracy.Value > maxAccuracy)
        {
            _logger.LogWarning(
                "GPS accuracy too low: {Accuracy}m (max: {MaxAccuracy}m) at ({Lat}, {Lon})",
                accuracy.Value, maxAccuracy, latitude, longitude);
            throw new ArgumentException(
                $"GPS accuracy too low ({accuracy:F1}m). Please wait for better signal (required: <{maxAccuracy}m).",
                nameof(accuracy));
        }

        // validate against municipality bounds if provided
        if (municipality != null)
        {
            if (!municipality.IsWithinBounds(latitude, longitude))
            {
                _logger.LogWarning(
                    "Coordinates outside {Municipality} operational area: ({Lat}, {Lon})",
                    municipality.Name, latitude, longitude);
                throw new ArgumentException($"Coordinates outside operational area ({municipality.Name}).");
            }
        }
        else
        {
            // fallback to global Palestine bounds for sanity check
            if (!Core.Constants.GeofencingConstants.IsWithinPalestine(latitude, longitude))
            {
                _logger.LogWarning(
                    "Coordinates outside Palestine region: ({Lat}, {Lon})",
                    latitude, longitude);
                throw new ArgumentException("Coordinates outside supported region.");
            }
        }

        // now check if user is in one of his assigned zones
        if (userId.HasValue)
        {
            var userZones = await _zoneRepository.GetUserZonesAsync(userId.Value);
            var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

            foreach (var zone in userZones)
            {
                if (zone.Boundary != null && zone.Boundary.Contains(point))
                {
                    return zone;
                }
            }

            _logger.LogWarning("User {UserId} is outside all their {Count} assigned zones at ({Lat}, {Lon})",
                userId, userZones.Count(), latitude, longitude);
            return null;
        }

        // validate against municipality-specific zones if provided
        if (municipalityId.HasValue)
        {
            return await _zoneRepository.ValidateLocationInMunicipalityAsync(latitude, longitude, municipalityId.Value);
        }

        return await _zoneRepository.ValidateLocationAsync(latitude, longitude);
    }

    public async Task<bool> IsPointInZoneAsync(double latitude, double longitude, int zoneId)
    {
        var zone = await _zoneRepository.GetByIdAsync(zoneId);
        if (zone == null || zone.Boundary == null)
            return false;

        var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        return zone.Boundary.Contains(point);
    }

    public Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2)
    {
        var distance = CalculateHaversineDistance(lat1, lon1, lat2, lon2);
        return Task.FromResult(distance);
    }

    public async Task ImportShapefileAsync(string filePath, int municipalityId)
    {
        // Verify municipality exists
        var municipality = await _municipalityRepository.GetByIdAsync(municipalityId);
        if (municipality == null)
            throw new ArgumentException($"Municipality with ID {municipalityId} not found", nameof(municipalityId));

        // check if file exist and get the base path
        var basePath = filePath.EndsWith(".shp", StringComparison.OrdinalIgnoreCase)
            ? filePath[..^4]
            : filePath;

        var shpPath = basePath + ".shp";
        if (!File.Exists(shpPath))
            throw new FileNotFoundException($"Shapefile not found at: {shpPath}", shpPath);

        try
        {
            // need this for reading shapefile encoding
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var zones = new List<Zone>();
            int featureIndex = 0;

            // read the shapefile
            using (var shapeFileDataReader = Shapefile.CreateDataReader(basePath, _geometryFactory))
            {
                while (shapeFileDataReader.Read())
                {
                    featureIndex++;
                    var geometry = shapeFileDataReader.Geometry;

                    if (geometry == null)
                        continue;

                    // fix ring orientation for sql server
                    var normalizedGeometry = NormalizeGeometry(geometry);

                    // get the zone info from shapefile attributes
                    // Note: DBF field names are truncated to 10 chars, so BlockName_Arabic becomes BlockName_ and BlockName_English becomes BlockName1
                    string zoneName = GetAttributeValue(shapeFileDataReader, new[] { "BlockName_", "BlockName_A", "QuarterNam", "NAME", "Name" })
                        ?? $"Zone {featureIndex}";
                    // Remove the number suffix like (1), (10) from Arabic names for cleaner display
                    if (zoneName.Contains("(") && zoneName.EndsWith(")"))
                    {
                        var parenIndex = zoneName.LastIndexOf('(');
                        zoneName = zoneName.Substring(0, parenIndex).Trim();
                    }
                    string zoneCode = GetAttributeValue(shapeFileDataReader, new[] { "BlockNumbe", "Quarter_Nu", "CODE", "Code", "ID" })
                        ?? $"ZONE-{featureIndex:D3}";
                    string englishName = GetAttributeValue(shapeFileDataReader, new[] { "BlockName1", "BlockName_E", "NAME_EN" });
                    string description = englishName ?? zoneName;
                    string district = GetAttributeValue(shapeFileDataReader, new[] { "Governorat", "Governor_1", "District" })
                        ?? municipality.Region ?? municipality.Name;

                    // get center of the zone
                    var centroid = normalizedGeometry.Centroid;

                    var zone = new Zone
                    {
                        MunicipalityId = municipalityId,
                        ZoneName = zoneName,
                        ZoneCode = zoneCode,
                        Description = description,
                        Boundary = normalizedGeometry,
                        BoundaryGeoJson = normalizedGeometry.AsText(),
                        CenterLatitude = centroid.Y,
                        CenterLongitude = centroid.X,
                        AreaSquareMeters = normalizedGeometry.Area * 111319.9 * 111319.9 * Math.Cos(centroid.Y * Math.PI / 180),
                        District = district,
                        Version = 1,
                        VersionDate = DateTime.UtcNow,
                        VersionNotes = "Imported from shapefile",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    zones.Add(zone);
                }
            }

            // save zones to database
            foreach (var zone in zones)
            {
                // check if zone already exists for this municipality
                var existing = await _zoneRepository.GetByCodeAndMunicipalityAsync(zone.ZoneCode, municipalityId);
                if (existing != null)
                {
                    // update it
                    existing.ZoneName = zone.ZoneName;
                    existing.Description = zone.Description;
                    existing.Boundary = zone.Boundary;
                    existing.BoundaryGeoJson = zone.BoundaryGeoJson;
                    existing.CenterLatitude = zone.CenterLatitude;
                    existing.CenterLongitude = zone.CenterLongitude;
                    existing.AreaSquareMeters = zone.AreaSquareMeters;
                    existing.Version++;
                    existing.VersionDate = DateTime.UtcNow;
                    existing.VersionNotes = "Updated from shapefile";
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _zoneRepository.UpdateAsync(existing);
                }
                else
                {
                    await _zoneRepository.AddAsync(zone);
                }
            }

            await _zoneRepository.SaveChangesAsync();
            _logger.LogInformation("Imported {Count} zones from shapefile for municipality {MunicipalityId}", zones.Count, municipalityId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import shapefile: {ex.Message}", ex);
        }
    }

    public async Task ImportGeoJsonAsync(string filePath, int municipalityId)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"GeoJSON file not found at: {filePath}", filePath);

        var geoJson = await File.ReadAllTextAsync(filePath);
        await ImportGeoJsonStringAsync(geoJson, municipalityId);
    }

    public async Task ImportGeoJsonStringAsync(string geoJson, int municipalityId)
    {
        // Verify municipality exists
        var municipality = await _municipalityRepository.GetByIdAsync(municipalityId);
        if (municipality == null)
            throw new ArgumentException($"Municipality with ID {municipalityId} not found", nameof(municipalityId));

        try
        {
            var geoJsonReader = new NetTopologySuite.IO.GeoJsonReader();
            var featureCollection = geoJsonReader.Read<NetTopologySuite.Features.FeatureCollection>(geoJson);

            if (featureCollection == null || featureCollection.Count == 0)
                throw new InvalidOperationException("No features found in GeoJSON");

            var zones = new List<Zone>();
            int featureIndex = 0;

            foreach (var feature in featureCollection)
            {
                featureIndex++;
                var geometry = feature.Geometry;

                if (geometry == null)
                    continue;

                // fix ring orientation for sql server
                var normalizedGeometry = NormalizeGeometry(geometry);

                // get zone info from feature properties
                string zoneName = GetGeoJsonProperty(feature, new[] { "name", "NAME", "zoneName", "BlockName_Arabic" })
                    ?? $"Zone {featureIndex}";
                string zoneCode = GetGeoJsonProperty(feature, new[] { "code", "CODE", "id", "ID", "zoneCode", "BlockNumber" })
                    ?? $"ZONE-{featureIndex:D3}";
                string description = GetGeoJsonProperty(feature, new[] { "description", "BlockName_English", "nameEn" })
                    ?? zoneName;
                string district = GetGeoJsonProperty(feature, new[] { "district", "governorate", "region" })
                    ?? municipality.Region ?? municipality.Name;

                // get center of the zone
                var centroid = normalizedGeometry.Centroid;

                var zone = new Zone
                {
                    MunicipalityId = municipalityId,
                    ZoneName = zoneName,
                    ZoneCode = zoneCode,
                    Description = description,
                    Boundary = normalizedGeometry,
                    BoundaryGeoJson = normalizedGeometry.AsText(),
                    CenterLatitude = centroid.Y,
                    CenterLongitude = centroid.X,
                    AreaSquareMeters = normalizedGeometry.Area * 111319.9 * 111319.9 * Math.Cos(centroid.Y * Math.PI / 180),
                    District = district,
                    Version = 1,
                    VersionDate = DateTime.UtcNow,
                    VersionNotes = "Imported from GeoJSON",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                zones.Add(zone);
            }

            // save zones to database
            foreach (var zone in zones)
            {
                // check if zone already exists for this municipality
                var existing = await _zoneRepository.GetByCodeAndMunicipalityAsync(zone.ZoneCode, municipalityId);
                if (existing != null)
                {
                    // update it
                    existing.ZoneName = zone.ZoneName;
                    existing.Description = zone.Description;
                    existing.Boundary = zone.Boundary;
                    existing.BoundaryGeoJson = zone.BoundaryGeoJson;
                    existing.CenterLatitude = zone.CenterLatitude;
                    existing.CenterLongitude = zone.CenterLongitude;
                    existing.AreaSquareMeters = zone.AreaSquareMeters;
                    existing.Version++;
                    existing.VersionDate = DateTime.UtcNow;
                    existing.VersionNotes = "Updated from GeoJSON";
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _zoneRepository.UpdateAsync(existing);
                }
                else
                {
                    await _zoneRepository.AddAsync(zone);
                }
            }

            await _zoneRepository.SaveChangesAsync();
            _logger.LogInformation("Imported {Count} zones from GeoJSON for municipality {MunicipalityId}", zones.Count, municipalityId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import GeoJSON: {ex.Message}", ex);
        }
    }

    // helper to get property from GeoJSON feature
    private string? GetGeoJsonProperty(NetTopologySuite.Features.IFeature feature, string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            if (feature.Attributes.Exists(name))
            {
                var value = feature.Attributes[name];
                if (value != null)
                    return value.ToString()?.Trim();
            }
        }
        return null;
    }

    // fix polygon ring orientation for SQL Server
    // SQL Server requires specific ring orientation: outer ring counter-clockwise, holes clockwise
    // found this solution on: https://gis.stackexchange.com/questions/119150/sql-server-geography-ring-orientation
    private Geometry NormalizeGeometry(Geometry geometry)
    {
        // points don't need fixing
        if (geometry is Point)
        {
            return geometry;
        }

        if (geometry is Polygon polygon)
        {
            // outer ring (shell) must be counter-clockwise
            var shell = polygon.Shell.Coordinates;
            if (!NetTopologySuite.Algorithm.Orientation.IsCCW(shell))
            {
                shell = shell.Reverse().ToArray();
            }

            // inner rings (holes) must be clockwise - opposite of shell
            var holes = polygon.Holes.Select(h =>
            {
                var holeCoords = h.Coordinates;
                if (NetTopologySuite.Algorithm.Orientation.IsCCW(holeCoords))
                {
                    holeCoords = holeCoords.Reverse().ToArray();
                }
                return _geometryFactory.CreateLinearRing(holeCoords);
            }).ToArray();

            return _geometryFactory.CreatePolygon(_geometryFactory.CreateLinearRing(shell), holes);
        }

        // handle multi-polygon by fixing each polygon
        if (geometry is MultiPolygon multiPolygon)
        {
            var polygons = multiPolygon.Geometries
                .Cast<Polygon>()
                .Select(p => (Polygon)NormalizeGeometry(p))
                .ToArray();
            return _geometryFactory.CreateMultiPolygon(polygons);
        }

        // other geometry types don't need orientation fix
        return geometry;
    }

    // try to get attribute value from shapefile using different possible names
    private string? GetAttributeValue(ShapefileDataReader reader, string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            try
            {
                var ordinal = reader.GetOrdinal(name);
                if (ordinal >= 0)
                {
                    var value = reader.GetValue(ordinal);
                    if (value != null && value != DBNull.Value)
                        return value.ToString()?.Trim();
                }
            }
            catch (IndexOutOfRangeException)
            {
                // column not found try next one
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unexpected error reading attribute {Name}", name);
            }
        }
        return null;
    }

    // calculate distance between two GPS points using Haversine formula
    // source: https://stackoverflow.com/questions/27928/calculate-distance-between-two-latitude-longitude-points-haversine-formula
    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0; // earth radius in kilometers

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        // haversine formula
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c * 1000; // convert to meters
    }

    // helper to convert degrees to radians
    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
