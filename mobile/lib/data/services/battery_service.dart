import 'dart:async';
import 'package:battery_plus/battery_plus.dart';
import 'package:flutter/foundation.dart';
import 'api_service.dart';

class BatteryService {
  static final BatteryService _instance = BatteryService._internal();
  factory BatteryService() => _instance;
  BatteryService._internal();

  final Battery _battery = Battery();
  final ApiService _apiService = ApiService();

  Timer? _monitoringTimer;
  StreamSubscription<BatteryState>? _batteryStateSubscription; // FIX: Store subscription
  int? _lastReportedLevel;
  DateTime? _lastReportTime;

  static const int lowBatteryThreshold = 20;
  static const Duration reportInterval = Duration(minutes: 15);

  // Start monitoring battery level
  void startMonitoring() {
    // Cancel existing monitoring if any
    stopMonitoring();

    // Check battery immediately
    _checkAndReportBattery();

    // Set up periodic checks every 5 minutes
    _monitoringTimer = Timer.periodic(
      const Duration(minutes: 5),
      (_) => _checkAndReportBattery(),
    );

    // FIX: Store the subscription so it can be cancelled
    _batteryStateSubscription = _battery.onBatteryStateChanged.listen((BatteryState state) {
      _checkAndReportBattery();
    });
  }

  // Stop monitoring
  void stopMonitoring() {
    _monitoringTimer?.cancel();
    _monitoringTimer = null;
    // FIX: Cancel battery state subscription to prevent memory leak
    _batteryStateSubscription?.cancel();
    _batteryStateSubscription = null;
  }

  // Check battery and report to backend if needed
  Future<void> _checkAndReportBattery() async {
    try {
      final level = await _battery.batteryLevel;
      final state = await _battery.batteryState;
      final isCharging = state == BatteryState.charging || state == BatteryState.full;

      // Determine if we should report
      bool shouldReport = false;

      // Report if battery level changed significantly (by 10%)
      if (_lastReportedLevel == null || (level - _lastReportedLevel!).abs() >= 10) {
        shouldReport = true;
      }

      // Report if battery is low and not charging
      if (level <= lowBatteryThreshold && !isCharging) {
        shouldReport = true;
      }

      // Report if enough time has passed since last report (15 minutes)
      if (_lastReportTime == null ||
          DateTime.now().difference(_lastReportTime!) >= reportInterval) {
        shouldReport = true;
      }

      if (shouldReport) {
        await _reportToBackend(level, isCharging);
      }
    } catch (e) {
      debugPrint('Error checking battery: $e');
    }
  }

  // Report battery status to backend
  Future<void> _reportToBackend(int level, bool isCharging) async {
    try {
      await _apiService.post(
        'users/battery-status',
        data: {
          'batteryLevel': level,
          'isLowBattery': level <= lowBatteryThreshold,
          'isCharging': isCharging,
          'timestamp': DateTime.now().toIso8601String(),
        },
      );

      _lastReportedLevel = level;
      _lastReportTime = DateTime.now();

      debugPrint('Battery status reported: $level% (charging: $isCharging)');
    } catch (e) {
      debugPrint('Error reporting battery: $e');
    }
  }

  // Get current battery level (for UI display)
  Future<int> getBatteryLevel() async {
    try {
      return await _battery.batteryLevel;
    } catch (e) {
      debugPrint('Error getting battery level: $e');
      return 100; // Default to 100 if error
    }
  }

  // Get current battery state
  Future<BatteryState> getBatteryState() async {
    try {
      return await _battery.batteryState;
    } catch (e) {
      debugPrint('Error getting battery state: $e');
      return BatteryState.unknown;
    }
  }

  // Check if battery is low
  Future<bool> isLowBattery() async {
    final level = await getBatteryLevel();
    final state = await getBatteryState();
    return level <= lowBatteryThreshold &&
           state != BatteryState.charging &&
           state != BatteryState.full;
  }
}
