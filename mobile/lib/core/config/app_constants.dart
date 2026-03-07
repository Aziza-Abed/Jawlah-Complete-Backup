// app-wide constants, grouped by feature
class AppConstants {
  AppConstants._();

  // GPS & Location
  static const int gpsTimeoutSeconds = 15;
  static const int gpsGeneralTimeoutSeconds = 30;
  static const double maxGpsAccuracyMeters = 100.0;

  // distance thresholds (must match backend)
  static const double nearbyDistanceMeters = 100.0;
  static const int warningDistanceMeters = 100;
  static const int hardRejectDistanceMeters = 500;

  // adaptive polling based on movement
  static const double stationaryThresholdMeters = 20.0;
  static const double slowMovementThresholdMeters = 100.0;
  static const Duration stationaryPollingInterval = Duration(minutes: 5);
  static const Duration slowMovementPollingInterval = Duration(minutes: 2);
  static const Duration fastMovementPollingInterval = Duration(seconds: 30);

  // offline GPS buffer
  static const int locationBufferMaxEntries = 1000;

  // task progress rate limit
  static const int progressUpdateIntervalMinutes = 5;

  // image compression
  static const int maxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
  static const int imageCompressionQuality = 70;
  static const int imageMaxDimension = 1024;

  // worker online threshold (must match backend)
  static const int onlineThresholdMinutes = 15;

  // background service
  static const int backgroundHttpTimeoutSeconds = 15;
  static const int geofenceCheckInCooldownMinutes = 30;
}
