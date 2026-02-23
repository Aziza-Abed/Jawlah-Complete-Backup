import 'package:hive/hive.dart';

part 'issue_local.g.dart';

@HiveType(typeId: 2)
class IssueLocal extends HiveObject {
  @HiveField(0)
  String? clientId; // Local ID before syncing

  @HiveField(1)
  int? serverId; // Server ID after syncing

  @HiveField(2)
  String title;

  @HiveField(3)
  String description;

  @HiveField(4)
  String type; // Infrastructure, Cleaning, Safety, etc.

  @HiveField(5)
  String severity; // Low, Medium, High, Critical

  @HiveField(6)
  int reportedByUserId;

  @HiveField(7)
  double latitude;

  @HiveField(8)
  double longitude;

  @HiveField(9)
  String? locationDescription;

  @HiveField(10)
  String? photoUrl; // Semicolon-separated for multiple photos

  @HiveField(11)
  DateTime reportedAt;

  @HiveField(12)
  bool isSynced;

  @HiveField(13)
  DateTime createdAt;

  @HiveField(14)
  DateTime? syncedAt;

  // Issue forwarding fields (SR15)
  @HiveField(15)
  int? forwardedToDepartmentId;

  @HiveField(16)
  String? forwardedToDepartmentName;

  @HiveField(17)
  DateTime? forwardedAt;

  @HiveField(18)
  String? forwardingNotes;

  // Version tracking for conflict resolution (nullable for safe Hive migration)
  @HiveField(19)
  int? syncVersion;

  // Issue status from server (Reported, UnderReview, Resolved, Dismissed)
  @HiveField(20)
  String? status;

  IssueLocal({
    this.clientId,
    this.serverId,
    required this.title,
    required this.description,
    required this.type,
    required this.severity,
    required this.reportedByUserId,
    required this.latitude,
    required this.longitude,
    this.locationDescription,
    this.photoUrl,
    required this.reportedAt,
    this.isSynced = false,
    required this.createdAt,
    this.syncedAt,
    this.forwardedToDepartmentId,
    this.forwardedToDepartmentName,
    this.forwardedAt,
    this.forwardingNotes,
    this.syncVersion,
    this.status,
  });

}
