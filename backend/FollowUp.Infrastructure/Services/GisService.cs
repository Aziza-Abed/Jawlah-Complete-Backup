using FollowUp.Core.Constants;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Infrastructure.Services;

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

    public async Task ImportShapefileAsync(string filePath, int municipalityId, GisFileType? fileType = null)
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
                    string? englishName = GetAttributeValue(shapeFileDataReader, new[] { "BlockName1", "BlockName_E", "NAME_EN" });
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
                        BoundaryGeoJson = WriteGeoJson(normalizedGeometry),
                        CenterLatitude = centroid.Y,
                        CenterLongitude = centroid.X,
                        AreaSquareMeters = normalizedGeometry.Area * AppConstants.MetersPerDegree * AppConstants.MetersPerDegree * Math.Cos(centroid.Y * Math.PI / 180),
                        District = district,
                        ZoneType = fileType,
                        Version = 1,
                        VersionDate = DateTime.UtcNow,
                        VersionNotes = "Imported from shapefile",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    zones.Add(zone);
                }
            }

            await SaveZonesToDatabaseAsync(zones, municipalityId, "shapefile");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import shapefile: {ex.Message}", ex);
        }
    }

    public async Task ImportGeoJsonAsync(string filePath, int municipalityId, GisFileType? fileType = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"GeoJSON file not found at: {filePath}", filePath);

        // Use FileShare.ReadWrite so this doesn't block concurrent uploads
        string geoJson;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
        {
            geoJson = await reader.ReadToEndAsync();
        }
        await ImportGeoJsonStringAsync(geoJson, municipalityId, fileType);
    }

    public async Task ImportGeoJsonStringAsync(string geoJson, int municipalityId, GisFileType? fileType = null)
    {
        // Verify municipality exists
        var municipality = await _municipalityRepository.GetByIdAsync(municipalityId);
        if (municipality == null)
            throw new ArgumentException($"Municipality with ID {municipalityId} not found", nameof(municipalityId));

        try
        {
            // Sanitize GeoJSON: remove non-standard properties (crs, name) that QGIS/GDAL add
            // These are not part of RFC 7946 and cause NTS GeoJsonReader to fail
            geoJson = SanitizeGeoJson(geoJson);

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

                // validate geometry
                if (!normalizedGeometry.IsValid)
                {
                    _logger.LogWarning("Feature {Index} has invalid geometry, attempting to fix", featureIndex);
                    normalizedGeometry = NetTopologySuite.Geometries.Utilities.GeometryFixer.Fix(normalizedGeometry);
                    if (!normalizedGeometry.IsValid)
                    {
                        _logger.LogError("Feature {Index} geometry cannot be fixed, skipping", featureIndex);
                        continue;
                    }
                }

                // get zone info from feature properties
                string zoneName = GetGeoJsonProperty(feature, new[] { "name", "NAME", "zoneName", "BlockName_Arabic", "QuarterNam" })
                    ?? $"Zone {featureIndex}";
                string zoneCode = GetGeoJsonProperty(feature, new[] { "code", "CODE", "id", "ID", "zoneCode", "BlockNumber", "Quarter_Nu" })
                    ?? $"ZONE-{featureIndex:D3}";
                string description = GetGeoJsonProperty(feature, new[] { "description", "BlockName_English", "nameEn", "QuarterN_1" })
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
                    BoundaryGeoJson = WriteGeoJson(normalizedGeometry),
                    CenterLatitude = centroid.Y,
                    CenterLongitude = centroid.X,
                    AreaSquareMeters = normalizedGeometry.Area * AppConstants.MetersPerDegree * AppConstants.MetersPerDegree * Math.Cos(centroid.Y * Math.PI / 180),
                    District = district,
                    ZoneType = fileType,
                    Version = 1,
                    VersionDate = DateTime.UtcNow,
                    VersionNotes = "Imported from GeoJSON",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                zones.Add(zone);
            }

            await SaveZonesToDatabaseAsync(zones, municipalityId, "GeoJSON");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import GeoJSON: {ex.Message}", ex);
        }
    }

    public async Task ImportBlocksFromGeoJsonAsync(string filePath, int municipalityId)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"GeoJSON file not found at: {filePath}", filePath);

        // Use FileShare.ReadWrite so this doesn't block concurrent uploads
        string geoJson;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8))
        {
            geoJson = await reader.ReadToEndAsync();
        }
        await ImportBlocksFromGeoJsonStringAsync(geoJson, municipalityId);
    }

    public async Task ImportBlocksFromGeoJsonStringAsync(string geoJson, int municipalityId)
    {
        // Verify municipality exists
        var municipality = await _municipalityRepository.GetByIdAsync(municipalityId);
        if (municipality == null)
            throw new ArgumentException($"Municipality with ID {municipalityId} not found", nameof(municipalityId));

        try
        {
            geoJson = SanitizeGeoJson(geoJson);

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

                // get block info from feature properties
                string blockNumber = GetGeoJsonProperty(feature, new[] { "BlockNumbe", "BlockNumber", "block_number" })
                    ?? featureIndex.ToString();
                string blockNameArabic = GetGeoJsonProperty(feature, new[] { "BlockName_", "BlockName_Arabic" })
                    ?? "";
                string blockNameEnglish = GetGeoJsonProperty(feature, new[] { "BlockName1", "BlockName_English" })
                    ?? "";
                string neighborhoodArabic = GetGeoJsonProperty(feature, new[] { "CommunityN", "Community_Arabic" })
                    ?? "";
                string neighborhoodEnglish = GetGeoJsonProperty(feature, new[] { "Communit_1", "Community_English" })
                    ?? "";

                // Create zone name: "BlockName - Neighborhood" or "Block X - Neighborhood"
                string zoneName;
                string zoneCode;

                if (!string.IsNullOrEmpty(blockNameArabic))
                {
                    zoneName = string.IsNullOrEmpty(neighborhoodArabic)
                        ? blockNameArabic
                        : $"{blockNameArabic} - {neighborhoodArabic}";
                }
                else
                {
                    zoneName = string.IsNullOrEmpty(neighborhoodArabic)
                        ? $"بلوك {blockNumber}"
                        : $"بلوك {blockNumber} - {neighborhoodArabic}";
                }

                zoneCode = $"BLK-{blockNumber}";

                string description = string.IsNullOrEmpty(blockNameEnglish)
                    ? $"Block {blockNumber} - {neighborhoodEnglish}"
                    : $"{blockNameEnglish} - {neighborhoodEnglish}";

                string district = neighborhoodArabic ?? municipality.Region ?? municipality.Name;

                // get center of the zone
                var centroid = normalizedGeometry.Centroid;

                var zone = new Zone
                {
                    MunicipalityId = municipalityId,
                    ZoneName = zoneName,
                    ZoneCode = zoneCode,
                    Description = description,
                    Boundary = normalizedGeometry,
                    BoundaryGeoJson = WriteGeoJson(normalizedGeometry),
                    CenterLatitude = centroid.Y,
                    CenterLongitude = centroid.X,
                    AreaSquareMeters = normalizedGeometry.Area * AppConstants.MetersPerDegree * AppConstants.MetersPerDegree * Math.Cos(centroid.Y * Math.PI / 180),
                    District = district,
                    ZoneType = GisFileType.Blocks,
                    Version = 1,
                    VersionDate = DateTime.UtcNow,
                    VersionNotes = "Imported from Blocks GeoJSON",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                zones.Add(zone);
            }

            await SaveZonesToDatabaseAsync(zones, municipalityId, "Blocks GeoJSON");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to import blocks from GeoJSON: {ex.Message}", ex);
        }
    }

    // helper to save/update zones in database (upsert by code + municipality)
    private async Task SaveZonesToDatabaseAsync(List<Zone> zones, int municipalityId, string source)
    {
        foreach (var zone in zones)
        {
            var existing = await _zoneRepository.GetByCodeAndMunicipalityAsync(zone.ZoneCode, municipalityId);
            if (existing != null)
            {
                existing.ZoneName = zone.ZoneName;
                existing.Description = zone.Description;
                existing.Boundary = zone.Boundary;
                existing.BoundaryGeoJson = zone.BoundaryGeoJson;
                existing.CenterLatitude = zone.CenterLatitude;
                existing.CenterLongitude = zone.CenterLongitude;
                existing.AreaSquareMeters = zone.AreaSquareMeters;
                existing.ZoneType = zone.ZoneType;
                existing.Version++;
                existing.VersionDate = DateTime.UtcNow;
                existing.VersionNotes = $"Updated from {source}";
                existing.UpdatedAt = DateTime.UtcNow;
                await _zoneRepository.UpdateAsync(existing);
            }
            else
            {
                await _zoneRepository.AddAsync(zone);
            }
        }

        await _zoneRepository.SaveChangesAsync();
        _logger.LogInformation("Imported {Count} zones from {Source} for municipality {MunicipalityId}", zones.Count, source, municipalityId);
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

    // helper to write geometry as GeoJSON string
    private string WriteGeoJson(Geometry geometry)
    {
        var writer = new NetTopologySuite.IO.GeoJsonWriter();
        return writer.Write(geometry);
    }

    // Remove non-standard root-level properties (crs, name) that QGIS/GDAL add.
    // These are not part of RFC 7946 and cause NTS GeoJsonReader to fail.
    private string SanitizeGeoJson(string geoJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(geoJson);
            var root = doc.RootElement;

            // Only sanitize FeatureCollections at root level
            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
                return geoJson;

            // Check if any non-standard properties exist to avoid unnecessary rewrite
            bool needsSanitize = false;
            string[] removeProps = { "crs", "name" };
            foreach (var prop in removeProps)
            {
                if (root.TryGetProperty(prop, out _))
                {
                    needsSanitize = true;
                    break;
                }
            }

            if (!needsSanitize)
                return geoJson;

            // Rebuild JSON without the non-standard properties
            using var stream = new System.IO.MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (Array.Exists(removeProps, p => p.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    property.WriteTo(writer);
                }
                writer.WriteEndObject();
            }

            var sanitized = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            _logger.LogInformation("Sanitized GeoJSON: removed non-standard properties (crs/name)");
            return sanitized;
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to sanitize GeoJSON, proceeding with original");
            return geoJson;
        }
    }

    // parse GeoJSON string to geometry
    public Geometry? ParseGeoJson(string geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
            return null;

        try
        {
            var reader = new NetTopologySuite.IO.GeoJsonReader();
            var geometry = reader.Read<Geometry>(geoJson);

            if (geometry != null)
            {
                // Normalize the geometry (fix ring orientation for SQL Server)
                geometry = NormalizeGeometry(geometry);
            }

            return geometry;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse GeoJSON: {GeoJson}", geoJson.Substring(0, Math.Min(100, geoJson.Length)));
            return null;
        }
    }
}
