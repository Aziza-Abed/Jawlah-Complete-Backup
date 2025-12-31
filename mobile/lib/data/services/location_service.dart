import 'package:geolocator/geolocator.dart';

import '../../core/errors/app_exception.dart';

class LocationService {
  LocationService._();

  static const LocationAccuracy defaultAccuracy = LocationAccuracy.medium;
  static const LocationAccuracy highAccuracy = LocationAccuracy.high;
  static const LocationAccuracy batteryOptimizedAccuracy = LocationAccuracy.low;

  static const Duration stationaryInterval = Duration(minutes: 5);
  static const Duration movingSlowInterval = Duration(minutes: 2);
  static const Duration movingFastInterval = Duration(seconds: 30);

  static const double stationaryThreshold = 20.0;
  static const double slowMovementThreshold = 100.0;

  static Future<bool> checkPermissions() async {
    try {
      final permission = await Geolocator.checkPermission();
      return permission == LocationPermission.whileInUse ||
          permission == LocationPermission.always;
    } catch (e) {
      return false;
    }
  }

  static Future<bool> requestPermissions() async {
    try {
      final permission = await Geolocator.requestPermission();
      return permission == LocationPermission.whileInUse ||
          permission == LocationPermission.always;
    } catch (e) {
      return false;
    }
  }

  static Future<bool> isPermissionDeniedForever() async {
    try {
      final permission = await Geolocator.checkPermission();
      return permission == LocationPermission.deniedForever;
    } catch (e) {
      return false;
    }
  }

  static Future<bool> isLocationServiceEnabled() async {
    try {
      return await Geolocator.isLocationServiceEnabled();
    } catch (e) {
      return false;
    }
  }

  static Future<bool> openLocationSettings() async {
    try {
      return await Geolocator.openLocationSettings();
    } catch (e) {
      return false;
    }
  }

  static Future<bool> openAppSettings() async {
    try {
      return await Geolocator.openAppSettings();
    } catch (e) {
      return false;
    }
  }

  static Future<Position?> getCurrentLocation({
    LocationAccuracy accuracy = defaultAccuracy,
  }) async {
    try {
      // check if GPS is turned on in the phone settings
      final serviceEnabled = await isLocationServiceEnabled();
      if (!serviceEnabled) {
        throw ValidationException('فشل الحصول على موقع GPS.');
      }

      // check if the app has permission to use location
      bool hasPermission = await checkPermissions();
      if (!hasPermission) {
        hasPermission = await requestPermissions();
        if (!hasPermission) {
          throw ValidationException('فشل الحصول على موقع GPS.');
        }
      }

      // get the actual location from the phone
      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: accuracy,
        timeLimit: const Duration(seconds: 30),
      );

      return position;
    } catch (e) {
      throw ValidationException('فشل الحصول على موقع GPS.');
    }
  }

  // get last known GPS position
  static Future<Position?> getLastKnownLocation() async {
    try {
      final hasPermission = await checkPermissions();
      if (!hasPermission) return null;

      return await Geolocator.getLastKnownPosition();
    } catch (e) {
      return null;
    }
  }

  // haversine-based distance between two points (meters)
  static double calculateDistance(
    double startLatitude,
    double startLongitude,
    double endLatitude,
    double endLongitude,
  ) {
    return Geolocator.distanceBetween(
      startLatitude,
      startLongitude,
      endLatitude,
      endLongitude,
    );
  }

  static Future<bool> isWithinRadius({
    required double targetLatitude,
    required double targetLongitude,
    required double radiusMeters,
  }) async {
    try {
      final position = await getCurrentLocation();
      if (position == null) return false;

      final distance = calculateDistance(
        position.latitude,
        position.longitude,
        targetLatitude,
        targetLongitude,
      );

      return distance <= radiusMeters;
    } catch (e) {
      return false;
    }
  }

  static Stream<Position> watchLocation({
    LocationAccuracy accuracy = defaultAccuracy,
    int distanceFilter = 50,
  }) {
    return Geolocator.getPositionStream(
      locationSettings: LocationSettings(
        accuracy: accuracy,
        distanceFilter: distanceFilter,
      ),
    );
  }

  static Duration getOptimalPollingInterval(
    Position? currentPosition,
    Position? previousPosition,
  ) {
    if (previousPosition == null) {
      return movingFastInterval;
    }

    final distance = calculateDistance(
      previousPosition.latitude,
      previousPosition.longitude,
      currentPosition!.latitude,
      currentPosition.longitude,
    );

    if (distance < stationaryThreshold) {
      return stationaryInterval;
    } else if (distance < slowMovementThreshold) {
      return movingSlowInterval;
    } else {
      return movingFastInterval;
    }
  }

  static bool isStationary(
    Position? currentPosition,
    Position? previousPosition,
  ) {
    if (previousPosition == null || currentPosition == null) {
      return false;
    }

    final distance = calculateDistance(
      previousPosition.latitude,
      previousPosition.longitude,
      currentPosition.latitude,
      currentPosition.longitude,
    );

    return distance < stationaryThreshold;
  }

  // summarize current location permission/service status
  static Future<LocationStatus> getLocationStatus() async {
    final serviceEnabled = await isLocationServiceEnabled();
    if (!serviceEnabled) {
      return LocationStatus.serviceDisabled;
    }

    final permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      return LocationStatus.permissionDenied;
    } else if (permission == LocationPermission.deniedForever) {
      return LocationStatus.permissionDeniedForever;
    }

    return LocationStatus.granted;
  }

  static String formatLocation(Position position) {
    return '${position.latitude.toStringAsFixed(4)}, ${position.longitude.toStringAsFixed(4)}';
  }

  static String formatAccuracy(Position position) {
    return 'دقة: ${position.accuracy.toStringAsFixed(0)} متر';
  }
}

enum LocationStatus {
  granted,
  permissionDenied,
  permissionDeniedForever,
  serviceDisabled,
}
