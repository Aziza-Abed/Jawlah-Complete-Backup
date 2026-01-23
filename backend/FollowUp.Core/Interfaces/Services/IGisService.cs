using FollowUp.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Core.Interfaces.Services;

public interface IGisService
{
    /// <summary>
    /// Validates a location against the user's municipality and assigned zones
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="userId">Optional user ID to check against assigned zones</param>
    /// <param name="municipalityId">Optional municipality ID to validate against specific municipality bounds</param>
    /// <param name="accuracy">Optional GPS accuracy in meters</param>
    /// <returns>The zone the location is in, or null if not in any zone</returns>
    Task<Zone?> ValidateLocationAsync(double latitude, double longitude, int? userId = null, int? municipalityId = null, double? accuracy = null);

    /// <summary>
    /// Checks if a point is within a specific zone
    /// </summary>
    Task<bool> IsPointInZoneAsync(double latitude, double longitude, int zoneId);

    /// <summary>
    /// Calculates the distance between two coordinates in meters
    /// </summary>
    Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);

    /// <summary>
    /// Imports zones from a shapefile for a specific municipality
    /// </summary>
    /// <param name="filePath">Path to the shapefile (.shp)</param>
    /// <param name="municipalityId">The municipality ID to associate imported zones with</param>
    Task ImportShapefileAsync(string filePath, int municipalityId);

    /// <summary>
    /// Imports zones from a GeoJSON file for a specific municipality
    /// </summary>
    /// <param name="filePath">Path to the GeoJSON file</param>
    /// <param name="municipalityId">The municipality ID to associate imported zones with</param>
    Task ImportGeoJsonAsync(string filePath, int municipalityId);

    /// <summary>
    /// Imports zones from a GeoJSON string for a specific municipality
    /// </summary>
    /// <param name="geoJson">GeoJSON content as string</param>
    /// <param name="municipalityId">The municipality ID to associate imported zones with</param>
    Task ImportGeoJsonStringAsync(string geoJson, int municipalityId);

    /// <summary>
    /// Imports blocks from a GeoJSON file as zones with neighborhood names
    /// </summary>
    /// <param name="filePath">Path to the blocks GeoJSON file</param>
    /// <param name="municipalityId">The municipality ID to associate imported blocks with</param>
    Task ImportBlocksFromGeoJsonAsync(string filePath, int municipalityId);

    /// <summary>
    /// Imports blocks from a GeoJSON string as zones with neighborhood names
    /// </summary>
    /// <param name="geoJson">GeoJSON content as string</param>
    /// <param name="municipalityId">The municipality ID to associate imported blocks with</param>
    Task ImportBlocksFromGeoJsonStringAsync(string geoJson, int municipalityId);
}
