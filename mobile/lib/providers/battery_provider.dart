import 'package:flutter/foundation.dart';
import '../data/services/battery_service.dart';
import 'base_controller.dart';

/// Provider for battery monitoring (UI display)
/// Uses BatteryService for actual monitoring - no duplicate API calls
class BatteryProvider extends BaseController {
  final BatteryService _batteryService = BatteryService();

  int _batteryLevel = 100;
  bool _isCharging = false;
  bool _hasShownLowBatteryWarning = false;

  static const int lowBatteryThreshold = 20;

  int get batteryLevel => _batteryLevel;
  bool get isCharging => _isCharging;
  bool get isLowBattery => _batteryLevel <= lowBatteryThreshold;

  /// Initialize battery monitoring for UI display
  Future<void> initialize() async {
    try {
      // Get initial values from BatteryService
      _batteryLevel = await _batteryService.getBatteryLevel();
      final state = await _batteryService.getBatteryState();
      _isCharging = state.name == 'charging' || state.name == 'full';

      notifyListeners();

      if (kDebugMode) {
        debugPrint('BatteryProvider initialized: $_batteryLevel% (charging: $_isCharging)');
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error initializing battery provider: $e');
      }
    }
  }

  /// Manually refresh battery level for UI
  Future<void> refreshBatteryLevel() async {
    try {
      _batteryLevel = await _batteryService.getBatteryLevel();
      final state = await _batteryService.getBatteryState();
      _isCharging = state.name == 'charging' || state.name == 'full';

      // Check for low battery warning
      if (_batteryLevel <= lowBatteryThreshold && !_isCharging && !_hasShownLowBatteryWarning) {
        _hasShownLowBatteryWarning = true;
      }

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
    // BatteryService lifecycle is managed by auth_manager
    super.dispose();
  }
}
