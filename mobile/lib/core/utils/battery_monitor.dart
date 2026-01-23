import 'dart:async';
import 'package:battery_plus/battery_plus.dart';
import 'package:flutter/foundation.dart';

/// Battery monitoring service for tracking device battery level
/// Provides battery percentage and low battery alerts
class BatteryMonitor {
  static final BatteryMonitor _instance = BatteryMonitor._internal();
  factory BatteryMonitor() => _instance;
  BatteryMonitor._internal();

  final Battery _battery = Battery();
  int _currentBatteryLevel = 100;
  BatteryState _batteryState = BatteryState.full;
  StreamSubscription<BatteryState>? _batteryStateSubscription;
  Timer? _batteryCheckTimer;

  // Low battery threshold (20%)
  static const int lowBatteryThreshold = 20;

  // Callbacks
  Function(int batteryLevel)? onBatteryLevelChanged;
  Function(int batteryLevel)? onLowBattery;

  int get currentBatteryLevel => _currentBatteryLevel;
  BatteryState get batteryState => _batteryState;
  bool get isLowBattery => _currentBatteryLevel <= lowBatteryThreshold;
  bool get isCharging => _batteryState == BatteryState.charging || _batteryState == BatteryState.full;

  /// Initialize battery monitoring
  Future<void> initialize() async {
    try {
      // Get initial battery level
      _currentBatteryLevel = await _battery.batteryLevel;
      _batteryState = await _battery.batteryState;

      if (kDebugMode) {
        debugPrint('Battery Monitor initialized: $_currentBatteryLevel% ($_batteryState)');
      }

      // Listen to battery state changes (charging, discharging, etc.)
      _batteryStateSubscription = _battery.onBatteryStateChanged.listen((state) {
        _batteryState = state;
        if (kDebugMode) {
          debugPrint('Battery state changed: $state');
        }
      });

      // Check battery level every 5 minutes
      _batteryCheckTimer = Timer.periodic(const Duration(minutes: 5), (_) async {
        await _checkBatteryLevel();
      });

      // Check immediately
      await _checkBatteryLevel();
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error initializing battery monitor: $e');
      }
    }
  }

  /// Check current battery level and trigger callbacks
  Future<void> _checkBatteryLevel() async {
    try {
      final previousLevel = _currentBatteryLevel;
      _currentBatteryLevel = await _battery.batteryLevel;

      // Trigger callback if level changed
      if (_currentBatteryLevel != previousLevel) {
        onBatteryLevelChanged?.call(_currentBatteryLevel);

        if (kDebugMode) {
          debugPrint('Battery level changed: $previousLevel% → $_currentBatteryLevel%');
        }
      }

      // Trigger low battery callback
      if (_currentBatteryLevel <= lowBatteryThreshold && !isCharging) {
        onLowBattery?.call(_currentBatteryLevel);

        if (kDebugMode) {
          debugPrint('⚠️ LOW BATTERY WARNING: $_currentBatteryLevel%');
        }
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error checking battery level: $e');
      }
    }
  }

  /// Get current battery level (forces refresh)
  Future<int> getBatteryLevel() async {
    try {
      _currentBatteryLevel = await _battery.batteryLevel;
      return _currentBatteryLevel;
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error getting battery level: $e');
      }
      return _currentBatteryLevel; // Return cached value
    }
  }

  /// Get battery state (charging, discharging, etc.)
  Future<BatteryState> getBatteryState() async {
    try {
      _batteryState = await _battery.batteryState;
      return _batteryState;
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error getting battery state: $e');
      }
      return _batteryState; // Return cached value
    }
  }

  /// Get battery icon based on level and state
  static String getBatteryIcon(int level, bool isCharging) {
    if (isCharging) {
      return '🔌'; // Charging
    } else if (level <= 20) {
      return '🪫'; // Low battery
    } else if (level <= 50) {
      return '🔋'; // Medium battery
    } else {
      return '🔋'; // High battery
    }
  }

  /// Get battery color based on level
  static int getBatteryColor(int level, bool isCharging) {
    if (isCharging) {
      return 0xFF4CAF50; // Green (charging)
    } else if (level <= 20) {
      return 0xFFE53935; // Red (low)
    } else if (level <= 50) {
      return 0xFFFFA726; // Orange (medium)
    } else {
      return 0xFF4CAF50; // Green (high)
    }
  }

  /// Dispose resources
  void dispose() {
    _batteryStateSubscription?.cancel();
    _batteryCheckTimer?.cancel();

    if (kDebugMode) {
      debugPrint('Battery Monitor disposed');
    }
  }
}
