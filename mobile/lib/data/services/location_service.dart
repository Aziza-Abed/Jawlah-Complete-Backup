import 'dart:async';

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

  static const int gpsTimeoutSeconds = 30;

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

  static Future<Position?> getCurrentLocation({
    LocationAccuracy accuracy = defaultAccuracy,
    bool validateAccuracy = true,
    double maxAccuracyMeters = 100.0,
  }) async {
    final serviceEnabled = await isLocationServiceEnabled();
    if (!serviceEnabled) {
      throw ValidationException('خدمة الموقع (GPS) معطلة. يرجى تفعيلها من إعدادات الهاتف.');
    }

    bool hasPermission = await checkPermissions();
    if (!hasPermission) {
      hasPermission = await requestPermissions();
      if (!hasPermission) {
        final deniedForever = await isPermissionDeniedForever();
        if (deniedForever) {
          throw ValidationException('تم رفض إذن الموقع بشكل دائم. يرجى تفعيله من إعدادات التطبيق.');
        }
        throw ValidationException('يرجى السماح للتطبيق بالوصول إلى موقعك.');
      }
    }

    try {
      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: accuracy,
        timeLimit: const Duration(seconds: gpsTimeoutSeconds),
      );

      // FIX 5: GPS ACCURACY VALIDATION
      if (validateAccuracy && position.accuracy > maxAccuracyMeters) {
        throw ValidationException(
          'دقة GPS منخفضة جداً (${position.accuracy.toInt()}م).\n'
          'انتقل إلى مكان مفتوح والحد الأقصى المسموح: ${maxAccuracyMeters.toInt()}م'
        );
      }

      return position;
    } on TimeoutException {
      throw ValidationException('انتهت مهلة تحديد الموقع. يرجى التأكد من وجود إشارة GPS جيدة.');
    } on ValidationException {
      rethrow; // Re-throw our custom accuracy validation error
    } catch (e) {
      throw ValidationException('فشل الحصول على موقع GPS: ${e.toString()}');
    }
  }

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
}
