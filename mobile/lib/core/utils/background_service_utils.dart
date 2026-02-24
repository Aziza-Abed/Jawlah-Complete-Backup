import 'dart:async';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:geolocator/geolocator.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:followup/core/config/api_config.dart';
import 'package:followup/core/utils/hive_init.dart';
import 'package:followup/data/services/tracking_service.dart';
import 'package:followup/data/services/location_service.dart';
import 'package:followup/data/services/zone_validation_service.dart';
import 'package:followup/data/repositories/local/zone_local_repository.dart';

class BackgroundServiceUtils {
  static final FlutterBackgroundService _service = FlutterBackgroundService();

  static Future<void> initializeService() async {
    const notificationChannelId = 'followup_tracking_channel';
    const notificationId = 888;

    final FlutterLocalNotificationsPlugin flutterLocalNotificationsPlugin =
        FlutterLocalNotificationsPlugin();

    // create Notification Channel for Android
    const AndroidNotificationChannel channel = AndroidNotificationChannel(
      notificationChannelId,
      'FollowUp Tracking Service',
      description: 'Background service for location tracking',
      importance: Importance.low,
    );

    // Create attendance notification channel (higher importance for user-visible events)
    const AndroidNotificationChannel attendanceChannel = AndroidNotificationChannel(
      'followup_attendance_channel',
      'تسجيل الحضور التلقائي',
      description: 'إشعارات تسجيل الحضور والانصراف التلقائي',
      importance: Importance.high,
    );

    await flutterLocalNotificationsPlugin
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(channel);

    await flutterLocalNotificationsPlugin
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(attendanceChannel);

    await _service.configure(
      androidConfiguration: AndroidConfiguration(
        onStart: onStart,
        autoStart: false, // Start manually after login
        isForegroundMode: true,
        notificationChannelId: notificationChannelId,
        initialNotificationTitle: 'FollowUp',
        initialNotificationContent: 'جاري تتبع الموقع...',
        foregroundServiceNotificationId: notificationId,
      ),
      iosConfiguration: IosConfiguration(
        autoStart: false,
        onForeground: onStart,
        onBackground: onIosBackground,
      ),
    );
  }

  static Future<void> startService() async {
    final isRunning = await _service.isRunning();
    if (!isRunning) {
      _service.startService();
    }
  }

  static Future<void> stopService() async {
    _service.invoke("stopService");
  }

  @pragma('vm:entry-point')
  static void onStart(ServiceInstance service) async {
    // ensuring basic flutter bindings
    WidgetsFlutterBinding.ensureInitialized();

    // initialize things needed in Isolate (Hive)
    await HiveInit.initialize();

    final trackingService = TrackingService();

    // initialize geofencing for automatic attendance
    final zoneService = ZoneValidationService(ZoneLocalRepository());
    final geofenceState = _GeofenceState();
    await geofenceState.loadState();

    // Initialize notifications for attendance events
    final notificationsPlugin = FlutterLocalNotificationsPlugin();
    await notificationsPlugin.initialize(
      const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
        iOS: DarwinInitializationSettings(),
      ),
    );

    // listen to stop command
    bool isRunning = true;
    service.on('stopService').listen((event) {
      isRunning = false;
      service.stopSelf();
    });

    if (service is AndroidServiceInstance) {
      service.on('setAsForeground').listen((event) {
        service.setAsForegroundService();
      });

      service.on('setAsBackground').listen((event) {
        service.setAsBackgroundService();
      });
    }

    await trackingService.connect();

    // check and request location permissions
    bool hasPermission = await _checkLocationPermission();
    if (!hasPermission) {
      if (kDebugMode) debugPrint('Location permission denied, stopping background service');
      service.stopSelf();
      return;
    }

    // start location update loop
    Position? previousPosition;
    Duration currentInterval = LocationService.movingFastInterval;

    // Store timer reference to prevent leaks
    Timer? activeTimer;

    void scheduleNextUpdate() {
      // Cancel any existing timer first to prevent leaks
      activeTimer?.cancel();
      activeTimer = null;

      // Don't schedule if service is stopping
      if (!isRunning) {
        return;
      }

      activeTimer = Timer(currentInterval, () async {
        if (!isRunning) {
          activeTimer?.cancel();
          activeTimer = null;
          return; // service stopped
        }

        try {
          // check location service status
          LocationPermission permission = await Geolocator.checkPermission();
          if (permission == LocationPermission.denied ||
              permission == LocationPermission.deniedForever) {
            if (service is AndroidServiceInstance) {
              service.setForegroundNotificationInfo(
                title: "تنبيه: توقف التتبع",
                content: "يرجى تفعيل صلاحيات الموقع لاستمرار العمل",
              );
            }
            activeTimer?.cancel();
            activeTimer = null;
            service.stopSelf();
            return;
          }

          bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
          if (!serviceEnabled) {
            if (service is AndroidServiceInstance) {
              service.setForegroundNotificationInfo(
                title: "تنبيه: الموقع غير مفعل",
                content: "يرجى تشغيل GPS لاستمرار العمل",
              );
            }
            scheduleNextUpdate();
            return;
          }

          // get current position
          Position position = await Geolocator.getCurrentPosition(
            desiredAccuracy: LocationService.defaultAccuracy,
            timeLimit: const Duration(seconds: 15),
          );

          // calculate next interval
          if (previousPosition != null) {
            currentInterval = LocationService.getOptimalPollingInterval(
              position,
              previousPosition,
            );
          }

          // send location to server
          await trackingService.sendLocation(
            position.latitude,
            position.longitude,
            speed: position.speed,
            accuracy: position.accuracy,
            heading: position.heading,
          );

          // auto-geofencing check after each position fix
          await _checkGeofence(
            position: position,
            geofenceState: geofenceState,
            zoneService: zoneService,
            notificationsPlugin: notificationsPlugin,
            service: service,
          );

          if (service is AndroidServiceInstance) {
            final movementStatus =
                LocationService.isStationary(position, previousPosition)
                    ? 'ثابت'
                    : 'متحرك';
            final attendanceStatus = geofenceState.isInside ? ' • حاضر' : '';
            service.setForegroundNotificationInfo(
              title: "FollowUp ($movementStatus$attendanceStatus)",
              content:
                  "آخر تحديث: ${DateTime.now().toLocal().toString().split('.')[0]}",
            );
          }

          previousPosition = position;
        } catch (e) {
          if (kDebugMode) debugPrint('Error in background loop: $e');
        }

        // Schedule next update with adaptive interval
        scheduleNextUpdate();
      });
    }

    // Start first update
    scheduleNextUpdate();
  }

  // check if worker entered/left assigned zone
  static Future<void> _checkGeofence({
    required Position position,
    required _GeofenceState geofenceState,
    required ZoneValidationService zoneService,
    required FlutterLocalNotificationsPlugin notificationsPlugin,
    required ServiceInstance service,
  }) async {
    try {
      // Check if position is inside any assigned zone
      final zone = await zoneService.validateLocationOffline(
        position.latitude,
        position.longitude,
      );

      final isInsideZone = zone != null;

      if (isInsideZone && !geofenceState.isInside) {
        // Worker entered a zone
        geofenceState.consecutiveInsideCount++;
        geofenceState.consecutiveOutsideCount = 0;

        // Require 2 consecutive readings to avoid GPS jitter false positives
        if (geofenceState.consecutiveInsideCount >= 2) {
          // Check if not already checked in recently (within 30 min)
          final now = DateTime.now();
          if (geofenceState.lastCheckInTime == null ||
              now.difference(geofenceState.lastCheckInTime!).inMinutes > 30) {
            // Auto check-in
            final success = await _autoCheckIn(position);
            if (success) {
              geofenceState.isInside = true;
              geofenceState.lastCheckInTime = now;
              geofenceState.consecutiveInsideCount = 0;
              await geofenceState.saveState();

              // show notification
              await _showAttendanceNotification(
                notificationsPlugin,
                'تم تسجيل حضورك تلقائياً',
                'تم تسجيل حضورك في ${zone.zoneName}',
              );

              // Notify main isolate
              service.invoke('attendanceUpdate', {
                'action': 'checkIn',
                'zoneName': zone.zoneName,
              });
            }
          }
        }
      } else if (!isInsideZone && geofenceState.isInside) {
        // Worker left all zones
        geofenceState.consecutiveOutsideCount++;
        geofenceState.consecutiveInsideCount = 0;

        // Require 2 consecutive outside readings
        if (geofenceState.consecutiveOutsideCount >= 2) {
          // Auto check-out
          final success = await _autoCheckOut(position);
          if (success) {
            geofenceState.isInside = false;
            geofenceState.consecutiveOutsideCount = 0;
            await geofenceState.saveState();

            // show notification
            await _showAttendanceNotification(
              notificationsPlugin,
              'تم تسجيل انصرافك تلقائياً',
              'تم تسجيل انصرافك - غادرت منطقة العمل',
            );

            // Notify main isolate
            service.invoke('attendanceUpdate', {
              'action': 'checkOut',
            });
          }
        }
      } else if (isInsideZone && geofenceState.isInside) {
        // Still inside — reset counters
        geofenceState.consecutiveOutsideCount = 0;
      } else {
        // Still outside — reset counters
        geofenceState.consecutiveInsideCount = 0;
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Geofence check error: $e');
    }
  }

  // call check-in API from background service
  static Future<bool> _autoCheckIn(Position position) async {
    try {
      const secureStorage = FlutterSecureStorage();
      final token = await secureStorage.read(key: 'jwt_token');
      if (token == null) return false;

      final dio = Dio(BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      ));

      final response = await dio.post(
        ApiConfig.checkIn,
        data: {
          'latitude': position.latitude,
          'longitude': position.longitude,
          'accuracy': position.accuracy,
        },
      );

      return response.statusCode == 200 && response.data['success'] == true;
    } catch (e) {
      if (kDebugMode) debugPrint('Auto check-in failed: $e');
      return false;
    }
  }

  // call check-out API from background service
  static Future<bool> _autoCheckOut(Position position) async {
    try {
      const secureStorage = FlutterSecureStorage();
      final token = await secureStorage.read(key: 'jwt_token');
      if (token == null) return false;

      final dio = Dio(BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      ));

      final response = await dio.post(
        ApiConfig.checkOut,
        data: {
          'latitude': position.latitude,
          'longitude': position.longitude,
          'accuracy': position.accuracy,
        },
      );

      return response.statusCode == 200 && response.data['success'] == true;
    } catch (e) {
      if (kDebugMode) debugPrint('Auto check-out failed: $e');
      return false;
    }
  }

  // show attendance notification to user
  static Future<void> _showAttendanceNotification(
    FlutterLocalNotificationsPlugin plugin,
    String title,
    String body,
  ) async {
    try {
      await plugin.show(
        889, // Unique ID for attendance notifications
        title,
        body,
        const NotificationDetails(
          android: AndroidNotificationDetails(
            'followup_attendance_channel',
            'تسجيل الحضور التلقائي',
            channelDescription: 'إشعارات تسجيل الحضور والانصراف التلقائي',
            importance: Importance.high,
            priority: Priority.high,
            icon: '@mipmap/ic_launcher',
          ),
          iOS: DarwinNotificationDetails(),
        ),
      );
    } catch (e) {
      if (kDebugMode) debugPrint('Failed to show attendance notification: $e');
    }
  }

  @pragma('vm:entry-point')
  static bool onIosBackground(ServiceInstance service) {
    WidgetsFlutterBinding.ensureInitialized();
    return true;
  }

  // check and request location permissions
  static Future<bool> _checkLocationPermission() async {
    try {
      LocationPermission permission = await Geolocator.checkPermission();

      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }

      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        if (kDebugMode) {
          debugPrint(
              'Location permission denied: ${permission == LocationPermission.deniedForever ? "forever" : "by user"}');
        }
        return false;
      }

      return true;
    } catch (e) {
      if (kDebugMode) debugPrint('Error checking location permission: $e');
      return false;
    }
  }
}

// geofence state persisted across background service restarts
class _GeofenceState {
  bool isInside = false;
  int consecutiveInsideCount = 0;
  int consecutiveOutsideCount = 0;
  DateTime? lastCheckInTime;

  Future<void> loadState() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      isInside = prefs.getBool('geofence_is_inside') ?? false;
      final lastCheckInMs = prefs.getInt('geofence_last_checkin');
      if (lastCheckInMs != null) {
        lastCheckInTime = DateTime.fromMillisecondsSinceEpoch(lastCheckInMs);
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Failed to load geofence state: $e');
    }
  }

  Future<void> saveState() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setBool('geofence_is_inside', isInside);
      if (lastCheckInTime != null) {
        await prefs.setInt(
          'geofence_last_checkin',
          lastCheckInTime!.millisecondsSinceEpoch,
        );
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Failed to save geofence state: $e');
    }
  }
}
