import 'package:hive/hive.dart';

part 'task_local.g.dart';

@HiveType(typeId: 1)
class TaskLocal extends HiveObject {
  @HiveField(0)
  String? clientId; // Local ID for offline tasks

  @HiveField(1)
  int taskId; // Server task ID

  @HiveField(2)
  String? title;

  @HiveField(3)
  String status;

  @HiveField(4)
  String? completionNotes;

  @HiveField(5)
  String? photoUrl;

  @HiveField(6)
  DateTime? completedAt;

  @HiveField(7)
  int syncVersion;

  @HiveField(8)
  bool isSynced;

  @HiveField(9)
  DateTime updatedAt;

  @HiveField(10)
  DateTime? syncedAt;

  @HiveField(11)
  String? description;

  @HiveField(12)
  String priority;

  @HiveField(13)
  DateTime? dueDate;

  @HiveField(14)
  int? zoneId;

  @HiveField(21)
  String? zoneName;

  @HiveField(15)
  double? latitude;

  @HiveField(16)
  double? longitude;

  @HiveField(17)
  String? locationDescription;

  @HiveField(18)
  String? taskType;

  @HiveField(19)
  bool requiresPhotoProof;

  @HiveField(20)
  int? estimatedDurationMinutes;

  @HiveField(22)
  int progressPercentage;

  @HiveField(23)
  String? progressNotes;

  TaskLocal({
    this.clientId,
    required this.taskId,
    this.title,
    required this.status,
    this.completionNotes,
    this.photoUrl,
    this.completedAt,
    this.syncVersion = 1,
    this.isSynced = false,
    required this.updatedAt,
    this.syncedAt,
    this.description,
    required this.priority,
    this.dueDate,
    this.zoneId,
    this.zoneName,
    this.latitude,
    this.longitude,
    this.locationDescription,
    this.taskType,
    this.requiresPhotoProof = false,
    this.estimatedDurationMinutes,
    this.progressPercentage = 0,
    this.progressNotes,
  });

  // Convert to sync DTO for uploading completed tasks
  Map<String, dynamic> toSyncDto() {
    return {
      'taskId': taskId,
      'status': status,
      'completionNotes': completionNotes,
      'photoUrl': photoUrl,
      'completedAt': completedAt?.toIso8601String(),
      'latitude': latitude,
      'longitude': longitude,
    };
  }
}
