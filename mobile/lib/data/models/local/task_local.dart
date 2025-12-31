import 'package:hive/hive.dart';

part 'task_local.g.dart';

@HiveType(typeId: 1)
class TaskLocal extends HiveObject {
  @HiveField(0)
  String? clientId; // Local ID before syncing

  @HiveField(1)
  int taskId; // Server task ID

  @HiveField(2)
  String? title;

  @HiveField(3)
  String status; // Pending, InProgress, Completed

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

  TaskLocal({
    this.clientId,
    required this.taskId,
    this.title,
    required this.status,
    this.completionNotes,
    this.photoUrl,
    this.completedAt,
    this.syncVersion = 0,
    this.isSynced = false,
    required this.updatedAt,
    this.syncedAt,
    this.description,
    this.priority = 'Medium',
    this.dueDate,
    this.zoneId,
    this.latitude,
    this.longitude,
    this.locationDescription,
    this.taskType,
    this.requiresPhotoProof = true,
    this.estimatedDurationMinutes,
  });

  // convert to sync DTO
  Map<String, dynamic> toSyncDto() {
    return {
      'clientId': clientId,
      'taskId': taskId,
      'title': title,
      'status': status,
      'completionNotes': completionNotes,
      'photoUrl': photoUrl,
      'completedAt': completedAt?.toIso8601String(),
      'syncVersion': syncVersion,
      'description': description,
      'priority': priority,
      'dueDate': dueDate?.toIso8601String(),
      'zoneId': zoneId,
      'latitude': latitude,
      'longitude': longitude,
      'locationDescription': locationDescription,
      'taskType': taskType,
      'requiresPhotoProof': requiresPhotoProof,
      'estimatedDurationMinutes': estimatedDurationMinutes,
    };
  }
}
