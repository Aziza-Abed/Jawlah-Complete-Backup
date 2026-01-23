import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:geolocator/geolocator.dart';
import 'package:followup/core/utils/hive_init.dart';
import 'package:followup/data/services/tracking_service.dart';
import 'package:followup/data/services/location_service.dart';

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

    await flutterLocalNotificationsPlugin
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(channel);

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
      debugPrint('Location permission denied, stopping background service');
      service.stopSelf();
      return;
    }

    // start location update loop
    Position? previousPosition;
    Duration currentInterval = LocationService.movingFastInterval;

    void scheduleNextUpdate(Timer? previousTimer) {
      previousTimer?.cancel();

      Timer(currentInterval, () async {
        if (!isRunning) {
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
            scheduleNextUpdate(null);
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

          previousPosition = position;

          // send location to server
          await trackingService.sendLocation(
            position.latitude,
            position.longitude,
            speed: position.speed,
            accuracy: position.accuracy,
            heading: position.heading,
          );

          if (service is AndroidServiceInstance) {
            final movementStatus =
                LocationService.isStationary(position, previousPosition)
                    ? 'ثابت'
                    : 'متحرك';
            service.setForegroundNotificationInfo(
              title: "FollowUp ($movementStatus)",
              content:
                  "آخر تحديث: ${DateTime.now().toLocal().toString().split('.')[0]}",
            );
          }
        } catch (e) {
          debugPrint('Error in background loop: $e');
          // Do NOT stop service on temporary errors, just log and retry next tick
        }

        // Schedule next update with adaptive interval
        scheduleNextUpdate(null);
      });
    }

    // Start first update
    scheduleNextUpdate(null);
  }

  @pragma('vm:entry-point')
  static bool onIosBackground(ServiceInstance service) {
    WidgetsFlutterBinding.ensureInitialized();
    return true;
  }

  // check and request location permissions
  static Future<bool> _checkLocationPermission() async {
    try {
      // check current permission status
      LocationPermission permission = await Geolocator.checkPermission();

      // request permission if needed
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }

      // if still denied or denied forever, return false
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        debugPrint(
            'Location permission denied: ${permission == LocationPermission.deniedForever ? "forever" : "by user"}');
        return false;
      }

      // permission granted (whileInUse or always)
      return true;
    } catch (e) {
      debugPrint('Error checking location permission: $e');
      return false;
    }
  }
}
