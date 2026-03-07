using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Services;

public interface IGisService
{
    // validates a location against the user's municipality and assigned zones
    Task<Zone?> ValidateLocationAsync(double latitude, double longitude, int? userId = null, int? municipalityId = null, double? accuracy = null);

    // checks if a point is within a specific zone
    Task<bool> IsPointInZoneAsync(double latitude, double longitude, int zoneId);

    // imports zones from a shapefile for a specific municipality
    Task ImportShapefileAsync(string filePath, int municipalityId, GisFileType? fileType = null);

    // imports zones from a GeoJSON file for a specific municipality
    Task ImportGeoJsonAsync(string filePath, int municipalityId, GisFileType? fileType = null);

    // imports zones from a GeoJSON string for a specific municipality
    Task ImportGeoJsonStringAsync(string geoJson, int municipalityId, GisFileType? fileType = null);

    // imports blocks from a GeoJSON file as zones with neighborhood names
    Task ImportBlocksFromGeoJsonAsync(string filePath, int municipalityId);

    // imports blocks from a GeoJSON string as zones with neighborhood names
    Task ImportBlocksFromGeoJsonStringAsync(string geoJson, int municipalityId);

    // parses a GeoJSON string into a NetTopologySuite Geometry object
    NetTopologySuite.Geometries.Geometry? ParseGeoJson(string geoJson);
}
