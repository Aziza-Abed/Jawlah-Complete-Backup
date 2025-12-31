using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Services;

public class GisService : IGisService
{
    private readonly IZoneRepository _zoneRepository;
    private readonly ILogger<GisService> _logger;
    private readonly GeometryFactory _geometryFactory;
    private readonly ICoordinateTransformation? _transformation;

    // palestine 1923 grid coordinate system for shapefile import
    private const string Palestine1923Wkt = @"PROJCS[""Palestine 1923 Palestine Grid"",
        GEOGCS[""GCS_Palestine_1923"",
            DATUM[""D_Palestine_1923"",
                SPHEROID[""Clarke_1880_Benoit"",6378300.789,293.4663155389811]],
            PRIMEM[""Greenwich"",0],
            UNIT[""Degree"",0.017453292519943295]],
        PROJECTION[""Cassini""],
        PARAMETER[""latitude_of_origin"",31.73409694444445],
        PARAMETER[""central_meridian"",35.21208055555556],
        PARAMETER[""false_easting"",170251.555],
        PARAMETER[""false_northing"",126867.909],
        UNIT[""Meter"",1]]";

    public GisService(IZoneRepository zoneRepository, ILogger<GisService> logger)
    {
        _zoneRepository = zoneRepository;
        _logger = logger;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

        try
        {
            var csFactory = new CoordinateSystemFactory();
            var ctFactory = new CoordinateTransformationFactory();

            var palestine1923 = csFactory.CreateFromWkt(Palestine1923Wkt);
            var wgs84 = GeographicCoordinateSystem.WGS84;

            _transformation = ctFactory.CreateFromCoordinateSystems(palestine1923, wgs84);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Projection Cassini is not supported by ProjNet. Coordinate transformation disabled. " +
                "Please use shapefiles already converted to WGS84 (EPSG:4326).");
            _transformation = null;
        }
    }

    public async Task<Zone?> ValidateLocationAsync(double latitude, double longitude, int? userId = null, double? accuracy = null)
    {
        // check if the coordinates are valid
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

        // check if the GPS accuracy is good
        if (accuracy.HasValue && accuracy.Value > Core.Constants.GeofencingConstants.MaxAcceptableAccuracyMeters)
        {
            _logger.LogWarning(
                "GPS accuracy too low: {Accuracy}m (max: {MaxAccuracy}m) at ({Lat}, {Lon})",
                accuracy.Value, Core.Constants.GeofencingConstants.MaxAcceptableAccuracyMeters, latitude, longitude);
            throw new ArgumentException(
                $"GPS accuracy too low ({accuracy:F1}m). Please wait for better signal (required: <{Core.Constants.GeofencingConstants.MaxAcceptableAccuracyMeters}m).",
                nameof(accuracy));
        }

        if (latitude == 0 && longitude == 0)
        {
            _logger.LogWarning("Received null island coordinates (0, 0) - likely GPS error");
            throw new ArgumentException("Invalid GPS coordinates received (0, 0). Please ensure GPS is enabled.");
        }

        // check if the location is inside Palestine
        if (latitude < Core.Constants.GeofencingConstants.MinLatitude ||
            latitude > Core.Constants.GeofencingConstants.MaxLatitude ||
            longitude < Core.Constants.GeofencingConstants.MinLongitude ||
            longitude > Core.Constants.GeofencingConstants.MaxLongitude)
        {
            _logger.LogWarning(
                "Coordinates outside Palestine operational area: ({Lat}, {Lon})",
                latitude, longitude);
            throw new ArgumentException("Coordinates outside operational area (Palestine region).");
        }

        // check if the location is inside the assigned zones
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
        var point1 = _geometryFactory.CreatePoint(new Coordinate(lon1, lat1));
        var point2 = _geometryFactory.CreatePoint(new Coordinate(lon2, lat2));

        var distance = CalculateHaversineDistance(lat1, lon1, lat2, lon2);

        return Task.FromResult(distance);
    }

    public async Task ImportShapefileAsync(string filePath)
    {
        // check if the file exists and prepare the path
        var basePath = filePath.EndsWith(".shp", StringComparison.OrdinalIgnoreCase)
            ? filePath[..^4]
            : filePath;

        var shpPath = basePath + ".shp";
        if (!File.Exists(shpPath))
            throw new FileNotFoundException($"Shapefile not found at: {shpPath}", shpPath);

        try
        {
            // register code pages for shapefile reading
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var shapeFileDataReader = Shapefile.CreateDataReader(basePath, _geometryFactory);
            var zones = new List<Zone>();
            int featureIndex = 0;

            while (shapeFileDataReader.Read())
            {
                featureIndex++;
                var geometry = shapeFileDataReader.Geometry;

                if (geometry == null)
                    continue;

                // transform coordinates from Palestine 1923 to WGS84
                var transformedGeometry = TransformGeometry(geometry);

                // extract attributes
                string zoneName = GetAttributeValue(shapeFileDataReader, new[] { "BlockName_E", "QuarterNam", "NAME", "Name" })
                    ?? $"Zone {featureIndex}";
                string zoneCode = GetAttributeValue(shapeFileDataReader, new[] { "BlockNumbe", "Quarter_Nu", "CODE", "Code", "ID" })
                    ?? $"ZONE-{featureIndex:D3}";
                string description = GetAttributeValue(shapeFileDataReader, new[] { "BlockName_A", "QuarterN_1", "NAME_AR" })
                    ?? zoneName;
                string district = GetAttributeValue(shapeFileDataReader, new[] { "Governorat", "District" })
                    ?? "Al-Bireh";

                // calculate center point
                var centroid = transformedGeometry.Centroid;

                var zone = new Zone
                {
                    ZoneName = zoneName,
                    ZoneCode = zoneCode,
                    Description = description,
                    Boundary = transformedGeometry,
                    BoundaryGeoJson = ConvertToGeoJson(transformedGeometry),
                    CenterLatitude = centroid.Y,
                    CenterLongitude = centroid.X,
                    AreaSquareMeters = transformedGeometry.Area * 111319.9 * 111319.9 * Math.Cos(centroid.Y * Math.PI / 180), // Approximate
                    District = district,
                    Version = 1,
                    VersionDate = DateTime.UtcNow,
                    VersionNotes = "Imported from shapefile",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                zones.Add(zone);
            }

            shapeFileDataReader.Close();

            // save to database
            foreach (var zone in zones)
            {
                // check if zone already exists by code
                var existing = await _zoneRepository.GetByCodeAsync(zone.ZoneCode);
                if (existing != null)
                {
                    // update existing zone
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
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import shapefile: {ex.Message}", ex);
        }
    }

    private Geometry TransformGeometry(Geometry geometry)
    {
        if (geometry is Point point)
        {
            var transformed = TransformCoordinate(point.Coordinate);
            return _geometryFactory.CreatePoint(transformed);
        }
        else if (geometry is Polygon polygon)
        {
            var shell = TransformCoordinates(polygon.Shell.Coordinates);
            // sQL Server requires counter-clockwise orientation for outer ring
            if (!NetTopologySuite.Algorithm.Orientation.IsCCW(shell))
            {
                shell = shell.Reverse().ToArray();
            }
            var holes = polygon.Holes.Select(h =>
            {
                var holeCoords = TransformCoordinates(h.Coordinates);
                // holes should be clockwise
                if (NetTopologySuite.Algorithm.Orientation.IsCCW(holeCoords))
                {
                    holeCoords = holeCoords.Reverse().ToArray();
                }
                return _geometryFactory.CreateLinearRing(holeCoords);
            }).ToArray();
            return _geometryFactory.CreatePolygon(_geometryFactory.CreateLinearRing(shell), holes);
        }
        else if (geometry is MultiPolygon multiPolygon)
        {
            var polygons = multiPolygon.Geometries.Cast<Polygon>().Select(p => (Polygon)TransformGeometry(p)).ToArray();
            return _geometryFactory.CreateMultiPolygon(polygons);
        }
        else if (geometry is LineString lineString)
        {
            var coords = TransformCoordinates(lineString.Coordinates);
            return _geometryFactory.CreateLineString(coords);
        }

        return geometry;
    }

    private Coordinate[] TransformCoordinates(Coordinate[] coordinates)
    {
        return coordinates.Select(TransformCoordinate).ToArray();
    }

    private Coordinate TransformCoordinate(Coordinate coord)
    {
        if (_transformation == null)
        {
            // no transformation available - assume coordinates are already WGS84
            return coord;
        }

        var result = _transformation.MathTransform.Transform(new[] { coord.X, coord.Y });
        return new Coordinate(result[0], result[1]);
    }

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
            catch
            {
                // column doesn't exist, try next
            }
        }
        return null;
    }

    private string ConvertToGeoJson(Geometry geometry)
    {
        return geometry.AsText();
    }

    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c * 1000;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
