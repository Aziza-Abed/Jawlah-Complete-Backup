import 'package:hive/hive.dart';

part 'zone_local.g.dart';

// local zone storage for offline validation
@HiveType(typeId: 5)
class ZoneLocal extends HiveObject {
  @HiveField(0)
  int zoneId;

  @HiveField(1)
  String zoneName;

  @HiveField(2)
  String zoneCode;

  @HiveField(3)
  String? description;

  @HiveField(4)
  double centerLatitude;

  @HiveField(5)
  double centerLongitude;

  @HiveField(6)
  double areaSquareMeters;

  @HiveField(7)
  String? district;

  @HiveField(8)
  int version;

  @HiveField(9)
  bool isActive;

  @HiveField(10)
  String? boundaryGeoJson;

  @HiveField(11)
  DateTime syncedAt;

  ZoneLocal({
    required this.zoneId,
    required this.zoneName,
    required this.zoneCode,
    this.description,
    required this.centerLatitude,
    required this.centerLongitude,
    required this.areaSquareMeters,
    this.district,
    required this.version,
    required this.isActive,
    this.boundaryGeoJson,
    required this.syncedAt,
  });

  factory ZoneLocal.fromJson(Map<String, dynamic> json) {
    return ZoneLocal(
      zoneId: json['zoneId'] as int,
      zoneName: json['zoneName'] as String? ?? '',
      zoneCode: json['zoneCode'] as String? ?? '',
      description: json['description'] as String?,
      centerLatitude: (json['centerLatitude'] as num?)?.toDouble() ?? 0.0,
      centerLongitude: (json['centerLongitude'] as num?)?.toDouble() ?? 0.0,
      areaSquareMeters: (json['areaSquareMeters'] as num?)?.toDouble() ?? 0.0,
      district: json['district'] as String?,
      version: json['version'] as int? ?? 1,
      isActive: json['isActive'] as bool? ?? true,
      boundaryGeoJson: json['boundaryGeoJson'] as String?,
      syncedAt: DateTime.now(),
    );
  }
}
