using Jawlah.Core.Entities;

namespace Jawlah.Core.Interfaces.Services;

public interface IGisService
{
    System.Threading.Tasks.Task<Zone?> ValidateLocationAsync(double latitude, double longitude);
    System.Threading.Tasks.Task<bool> IsPointInZoneAsync(double latitude, double longitude, int zoneId);
    System.Threading.Tasks.Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);
    System.Threading.Tasks.Task ImportShapefileAsync(string filePath);
}
