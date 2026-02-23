import 'package:flutter/foundation.dart';
import 'package:hive/hive.dart';
import '../../models/local/zone_local.dart';

/// Section 3.5.2: Local zone repository for offline validation
class ZoneLocalRepository {
  static const String _boxName = 'zone_local';

  Box<ZoneLocal> get _box => Hive.box<ZoneLocal>(_boxName);

  /// Get all cached zones
  Future<List<ZoneLocal>> getAllZones() async {
    return _box.values.where((z) => z.isActive).toList();
  }

  /// Get zone by ID
  Future<ZoneLocal?> getZoneById(int zoneId) async {
    try {
      return _box.values.firstWhere((z) => z.zoneId == zoneId);
    } catch (_) {
      return null;
    }
  }

  /// Save or update zones from server
  Future<void> saveZones(List<ZoneLocal> zones) async {
    // Clear existing zones and save new ones
    await _box.clear();
    for (var zone in zones) {
      await _box.put(zone.zoneId.toString(), zone);
    }
    if (kDebugMode) {
      debugPrint('ZoneLocalRepo: Saved ${zones.length} zones for offline validation');
    }
  }

  /// Update a single zone
  Future<void> updateZone(ZoneLocal zone) async {
    await _box.put(zone.zoneId.toString(), zone);
  }

  /// Check if zones are cached
  Future<bool> hasZones() async {
    return _box.isNotEmpty;
  }

  /// Get zones count
  Future<int> getZonesCount() async {
    return _box.length;
  }

  /// Clear all zones
  Future<void> clearAll() async {
    await _box.clear();
  }
}
