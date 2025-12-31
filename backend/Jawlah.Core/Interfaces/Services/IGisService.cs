using Jawlah.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Core.Interfaces.Services;

public interface IGisService
{
    Task<Zone?> ValidateLocationAsync(double latitude, double longitude, int? userId = null, double? accuracy = null);
    Task<bool> IsPointInZoneAsync(double latitude, double longitude, int zoneId);
    Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);
    Task ImportShapefileAsync(string filePath);
}
