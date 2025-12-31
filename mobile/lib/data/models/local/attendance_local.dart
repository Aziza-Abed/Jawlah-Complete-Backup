import 'package:hive/hive.dart';

part 'attendance_local.g.dart';

@HiveType(typeId: 0)
class AttendanceLocal extends HiveObject {
  @HiveField(0)
  String? clientId; // Local ID before syncing

  @HiveField(1)
  int? serverId; // Server ID after syncing

  @HiveField(2)
  int userId;

  @HiveField(3)
  DateTime checkInTime;

  @HiveField(4)
  DateTime? checkOutTime;

  @HiveField(5)
  double checkInLatitude;

  @HiveField(6)
  double checkInLongitude;

  @HiveField(7)
  double? checkOutLatitude;

  @HiveField(8)
  double? checkOutLongitude;

  @HiveField(9)
  bool isValidated;

  @HiveField(10)
  String? validationMessage;

  @HiveField(11)
  bool isSynced;

  @HiveField(12)
  DateTime createdAt;

  @HiveField(13)
  DateTime? syncedAt;

  AttendanceLocal({
    this.clientId,
    this.serverId,
    required this.userId,
    required this.checkInTime,
    this.checkOutTime,
    required this.checkInLatitude,
    required this.checkInLongitude,
    this.checkOutLatitude,
    this.checkOutLongitude,
    this.isValidated = false,
    this.validationMessage,
    this.isSynced = false,
    required this.createdAt,
    this.syncedAt,
  });

  // convert to sync DTO
  Map<String, dynamic> toSyncDto() {
    return {
      'clientId': clientId,
      'userId': userId,
      'checkInTime': checkInTime.toIso8601String(),
      'checkOutTime': checkOutTime?.toIso8601String(),
      'checkInLatitude': checkInLatitude,
      'checkInLongitude': checkInLongitude,
      'checkOutLatitude': checkOutLatitude,
      'checkOutLongitude': checkOutLongitude,
      'isValidated': isValidated,
      'validationMessage': validationMessage,
    };
  }
}

