import 'package:flutter/foundation.dart';
import '../core/utils/battery_monitor.dart';
import '../data/services/api_service.dart';
import 'base_controller.dart';

/// Provider for battery monitoring and reporting
class BatteryProvider extends BaseController {
  final BatteryMonitor _batteryMonitor = BatteryMonitor();
  final ApiService _apiService = ApiService();

  int _batteryLevel = 100;
  bool _isCharging = false;
  bool _hasShownLowBatteryWarning = false;
  DateTime? _lastBatteryReport;

  int get batteryLevel => _batteryLevel;
  bool get isCharging => _isCharging;
  bool get isLowBattery => _batteryLevel <= BatteryMonitor.lowBatteryThreshold;

  /// Initialize battery monitoring
  Future<void> initialize() async {
    try {
      // Initialize monitor
      await _batteryMonitor.initialize();

      // Get initial values
      _batteryLevel = _batteryMonitor.currentBatteryLevel;
      _isCharging = _batteryMonitor.isCharging;

      // Set up callbacks
      _batteryMonitor.onBatteryLevelChanged = _onBatteryLevelChanged;
      _batteryMonitor.onLowBattery = _onLowBattery;

      notifyListeners();

      if (kDebugMode) {
        debugPrint('BatteryProvider initialized: $_batteryLevel% (charging: $_isCharging)');
      }

      // Report battery status to server
      await _reportBatteryStatus();
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error initializing battery provider: $e');
      }
    }
  }

  /// Called when battery level changes
  void _onBatteryLevelChanged(int level) {
    _batteryLevel = level;
    _isCharging = _batteryMonitor.isCharging;
    notifyListeners();

    // Report to server if level changed significantly (every 10%)
    if (_shouldReportBattery()) {
      _reportBatteryStatus();
    }
  }

  /// Called when battery is low
  void _onLowBattery(int level) {
    if (!_hasShownLowBatteryWarning) {
      _hasShownLowBatteryWarning = true;
      // Callback will be handled by UI to show notification
      notifyListeners();
    }

    // Report low battery to server
    _reportBatteryStatus(isLowBatteryAlert: true);
  }

  /// Check if we should report battery status to server
  bool _shouldReportBattery() {
    // Report every 30 minutes
    if (_lastBatteryReport == null) return true;

    final timeSinceLastReport = DateTime.now().difference(_lastBatteryReport!);
    return timeSinceLastReport.inMinutes >= 30;
  }

  /// Report battery status to server
  Future<void> _reportBatteryStatus({bool isLowBatteryAlert = false}) async {
    try {
      await _apiService.post(
        'users/battery-status',
        data: {
          'batteryLevel': _batteryLevel,
          'isCharging': _isCharging,
          'isLowBattery': isLowBattery,
          'timestamp': DateTime.now().toIso8601String(),
        },
      );

      _lastBatteryReport = DateTime.now();

      if (kDebugMode) {
        debugPrint('Battery status reported: $_batteryLevel% (low alert: $isLowBatteryAlert)');
      }
    } catch (e) {
      // Silently fail - not critical
      if (kDebugMode) {
        debugPrint('Failed to report battery status: $e');
      }
    }
  }

  /// Manually refresh battery level
  Future<void> refreshBatteryLevel() async {
    try {
      _batteryLevel = await _batteryMonitor.getBatteryLevel();
      _isCharging = _batteryMonitor.isCharging;
      notifyListeners();
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error refreshing battery level: $e');
      }
    }
  }

  /// Reset low battery warning flag
  void resetLowBatteryWarning() {
    _hasShownLowBatteryWarning = false;
  }

  @override
  void dispose() {
    _batteryMonitor.dispose();
    super.dispose();
  }
}
