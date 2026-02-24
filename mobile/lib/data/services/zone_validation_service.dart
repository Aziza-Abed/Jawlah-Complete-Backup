import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:maps_toolkit/maps_toolkit.dart';
import '../models/local/zone_local.dart';
import '../repositories/local/zone_local_repository.dart';

// local zone validation service using preloaded shapefiles
class ZoneValidationService {
  final ZoneLocalRepository _zoneRepo;

  ZoneValidationService(this._zoneRepo);

  // validate if a GPS point is inside any of the user's assigned zones
  // returns the zone if valid, null if outside all zones
  Future<ZoneLocal?> validateLocationOffline(
    double latitude,
    double longitude,
  ) async {
    final zones = await _zoneRepo.getAllZones();

    if (zones.isEmpty) {
      if (kDebugMode) {
        debugPrint('ZoneValidation: No zones cached for offline validation');
      }
      return null;
    }

    final point = LatLng(latitude, longitude);

    for (var zone in zones) {
      if (zone.boundaryGeoJson == null || zone.boundaryGeoJson!.isEmpty) {
        continue;
      }

      try {
        final polygon = _parseGeoJsonToPolygon(zone.boundaryGeoJson!);
        if (polygon != null && _isPointInPolygon(point, polygon)) {
          if (kDebugMode) {
            debugPrint('ZoneValidation: Point is inside zone ${zone.zoneName}');
          }
          return zone;
        }
      } catch (e) {
        if (kDebugMode) {
          debugPrint('ZoneValidation: Failed to parse zone ${zone.zoneId}: $e');
        }
      }
    }

    if (kDebugMode) {
      debugPrint('ZoneValidation: Point ($latitude, $longitude) is outside all zones');
    }
    return null;
  }

  // parse GeoJSON to list of LatLng points
  List<LatLng>? _parseGeoJsonToPolygon(String geoJson) {
    try {
      final json = jsonDecode(geoJson);

      // Handle different GeoJSON formats
      List<dynamic>? coordinates;

      if (json['type'] == 'Polygon') {
        // Direct Polygon type
        coordinates = json['coordinates']?[0] as List<dynamic>?;
      } else if (json['type'] == 'Feature') {
        // Feature with Polygon geometry
        final geometry = json['geometry'];
        if (geometry != null && geometry['type'] == 'Polygon') {
          coordinates = geometry['coordinates']?[0] as List<dynamic>?;
        }
      } else if (json['type'] == 'MultiPolygon') {
        // MultiPolygon - use first polygon
        coordinates = json['coordinates']?[0]?[0] as List<dynamic>?;
      }

      if (coordinates == null || coordinates.isEmpty) {
        return null;
      }

      // Convert to LatLng list (GeoJSON uses [longitude, latitude])
      return coordinates.map((coord) {
        final lng = (coord[0] as num).toDouble();
        final lat = (coord[1] as num).toDouble();
        return LatLng(lat, lng);
      }).toList();
    } catch (e) {
      if (kDebugMode) {
        debugPrint('ZoneValidation: GeoJSON parse error: $e');
      }
      return null;
    }
  }

  // check if point is inside polygon using maps_toolkit
  bool _isPointInPolygon(LatLng point, List<LatLng> polygon) {
    return PolygonUtil.containsLocation(point, polygon, true);
  }

  // check if zones are available for offline validation
  Future<bool> hasOfflineZones() async {
    return await _zoneRepo.hasZones();
  }
}
