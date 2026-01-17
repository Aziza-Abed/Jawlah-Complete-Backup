import 'dart:async';
import 'package:battery_plus/battery_plus.dart';
import 'package:flutter/foundation.dart';
import 'api_service.dart';

// service to monitor battery and report to server when low
class BatteryService {
  static final BatteryService instance = BatteryService._init();
  factory BatteryService() => instance;
  BatteryService._init();

  final Battery _battery = Battery();
  final ApiService _api = ApiService();

  // battery threshold
  static const int lowBatteryThreshold = 20;

  // track if we already sent notification (dont spam)
  bool _alreadyNotified = false;
  int _lastReportedLevel = -1;

  StreamSubscription<BatteryState>? _subscription;

  // start monitoring battery
  void startMonitoring() {
    // check battery level now
    _checkBattery();

    // listen to battery state changes
    _subscription = _battery.onBatteryStateChanged.listen((state) {
      _checkBattery();
    });
  }

  // stop monitoring
  void stopMonitoring() {
    _subscription?.cancel();
    _subscription = null;
    _alreadyNotified = false;
    _lastReportedLevel = -1;
  }

  // check battery level and report if low
  Future<void> _checkBattery() async {
    try {
      final level = await _battery.batteryLevel;

      // only report if battery is low
      if (level <= lowBatteryThreshold) {
        // dont send again if we already notified at this level
        if (_alreadyNotified && _lastReportedLevel == level) {
          return;
        }

        // report to server
        await _reportBatteryLevel(level);
        _alreadyNotified = true;
        _lastReportedLevel = level;
      } else {
        // battery is ok reset the flag
        _alreadyNotified = false;
        _lastReportedLevel = -1;
      }
    } catch (e) {
      debugPrint('Error checking battery: $e');
    }
  }

  // send battery level to server
  Future<void> _reportBatteryLevel(int level) async {
    try {
      await _api.post('/users/battery', data: {
        'batteryLevel': level,
      });
      debugPrint('Reported low battery to server: $level%');
    } catch (e) {
      debugPrint('Failed to report battery: $e');
    }
  }

  // get current battery level
  Future<int> getBatteryLevel() async {
    return await _battery.batteryLevel;
  }
}
