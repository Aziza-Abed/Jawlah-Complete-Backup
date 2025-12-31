import '../../core/config/api_config.dart';
import '../../core/errors/app_exception.dart';
import '../models/worker_location.dart';
import 'api_service.dart';
import 'location_service.dart';

class LocationHistoryService {
  final ApiService _apiService = ApiService();

  // get location history for a specific user and date
  Future<List<WorkerLocation>> getHistory({
    required int userId,
    required DateTime date,
  }) async {
    try {
      final response = await _apiService.get(
        ApiConfig.getTrackingHistoryUrl(userId),
        queryParameters: {
          'date': date.toIso8601String(),
        },
      );

      if (response.data['success'] == true) {
        final List<dynamic> data = response.data['data'] as List<dynamic>;
        return data.map((json) {
          return WorkerLocation(
            userId: userId,
            fullName: 'Worker #$userId',
            role: 'Worker',
            latitude: (json['latitude'] as num).toDouble(),
            longitude: (json['longitude'] as num).toDouble(),
            speed: json['speed'] != null
                ? (json['speed'] as num).toDouble()
                : null,
            accuracy: json['accuracy'] != null
                ? (json['accuracy'] as num).toDouble()
                : null,
            lastUpdate: json['timestamp'] != null
                ? DateTime.parse(json['timestamp'] as String)
                : null,
          );
        }).toList();
      }

      return [];
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل تحميل سجل المواقع');
    }
  }

  // calculate total distance traveled (in meters)
  double calculateTotalDistance(List<WorkerLocation> locations) {
    if (locations.length < 2) return 0.0;

    double totalDistance = 0.0;
    for (int i = 0; i < locations.length - 1; i++) {
      final distance = LocationService.calculateDistance(
        locations[i].latitude,
        locations[i].longitude,
        locations[i + 1].latitude,
        locations[i + 1].longitude,
      );
      totalDistance += distance;
    }

    return totalDistance;
  }
}
