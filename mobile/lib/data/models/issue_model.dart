import '../../core/utils/date_formatter.dart';
import 'local/issue_local.dart';

class IssueModel {
  final int issueId;
  final String title;
  final String description;
  final String type; // Type of issue (e.g., "Equipment", "Road")
  final String severity; // How serious is it? (e.g., "High", "Low")
  final String status; // Current status (e.g., "New", "Forwarded", "Resolved")
  final int reportedByUserId; // Who reported it
  final String? reportedByName; // Worker's name (optional)
  final String? location; // Text description of location (optional)
  final double? latitude; // GPS coordinate - can be null if not available
  final double? longitude; // GPS coordinate - can be null if not available
  final String? photoUrl; // Legacy single photo URL (optional)
  final List<String> photos; // Multiple photos from new API
  final String? resolutionNotes; // How was it fixed? (optional)
  final DateTime? resolvedAt; // When was it fixed? (optional)
  final DateTime createdAt; // When was it reported
  final DateTime updatedAt; // Last update time

  // issue forwarding info
  final int? forwardedToDepartmentId;
  final String? forwardedToDepartmentName;
  final DateTime? forwardedAt;
  final String? forwardingNotes;

  IssueModel({
    required this.issueId,
    required this.title,
    required this.description,
    required this.type,
    required this.severity,
    required this.status,
    required this.reportedByUserId,
    this.reportedByName,
    this.location,
    this.latitude,
    this.longitude,
    this.photoUrl,
    this.photos = const [],
    this.resolutionNotes,
    this.resolvedAt,
    required this.createdAt,
    required this.updatedAt,
    this.forwardedToDepartmentId,
    this.forwardedToDepartmentName,
    this.forwardedAt,
    this.forwardingNotes,
  });

  // create IssueModel from local Hive storage (for offline display)
  factory IssueModel.fromLocal(IssueLocal local) {
    return IssueModel(
      issueId: local.serverId ?? (-1 * local.createdAt.millisecondsSinceEpoch),
      title: local.title,
      description: local.description,
      type: local.type,
      severity: local.severity,
      status: local.status ?? 'New',
      reportedByUserId: local.reportedByUserId,
      latitude: local.latitude,
      longitude: local.longitude,
      location: local.locationDescription,
      photoUrl: local.photoUrl?.split(';').firstOrNull,
      createdAt: local.createdAt,
      updatedAt: local.syncedAt ?? local.createdAt,
      forwardedToDepartmentId: local.forwardedToDepartmentId,
      forwardedToDepartmentName: local.forwardedToDepartmentName,
      forwardedAt: local.forwardedAt,
      forwardingNotes: local.forwardingNotes,
    );
  }

  factory IssueModel.fromJson(Map<String, dynamic> json) {
    return IssueModel(
      issueId: json['issueId'] as int? ?? json['IssueId'] as int? ?? 0,
      title: json['title'] as String? ?? json['Title'] as String? ?? '',
      description:
          json['description'] as String? ?? json['Description'] as String? ?? '',
      type: json['type'] as String? ?? json['Type'] as String? ?? 'Other',
      severity: json['severity'] as String? ??
          json['Severity'] as String? ??
          'Medium',
      status:
          json['status'] as String? ?? json['Status'] as String? ?? 'New',
      reportedByUserId:
          json['reportedByUserId'] as int? ?? json['ReportedByUserId'] as int? ?? 0,
      reportedByName: json['reportedByName'] as String? ??
          json['ReportedByName'] as String?,
      location:
          json['location'] as String? ?? json['locationDescription'] as String?,

      latitude: (json['latitude'] ?? json['Latitude']) != null
          ? ((json['latitude'] ?? json['Latitude']) as num).toDouble()
          : null,
      longitude: (json['longitude'] ?? json['Longitude']) != null
          ? ((json['longitude'] ?? json['Longitude']) as num).toDouble()
          : null,
      photoUrl: json['photoUrl'] as String? ?? json['PhotoUrl'] as String?,
      photos: (json['photos'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          (json['Photos'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      resolutionNotes: json['resolutionNotes'] as String? ??
          json['ResolutionNotes'] as String?,
      resolvedAt: DateFormatter.tryParseUtc(
          json['resolvedAt'] ?? json['ResolvedAt']),
      createdAt: DateFormatter.tryParseUtc(
              json['createdAt'] ?? json['reportedAt']) ??
          DateTime.now().toUtc(),
      updatedAt: DateFormatter.tryParseUtc(
              json['updatedAt'] ?? json['syncTime']) ??
          DateTime.now().toUtc(),
      forwardedToDepartmentId: json['forwardedToDepartmentId'] as int?,
      forwardedToDepartmentName: json['forwardedToDepartmentName'] as String?,
      forwardedAt: DateFormatter.tryParseUtc(json['forwardedAt']),
      forwardingNotes: json['forwardingNotes'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'issueId': issueId,
      'title': title,
      'description': description,
      'type': type,
      'severity': severity,
      'status': status,
      'reportedByUserId': reportedByUserId,
      'reportedByName': reportedByName,
      'location': location,
      'latitude': latitude,
      'longitude': longitude,
      'photoUrl': photoUrl,
      'photos': photos,
      'resolutionNotes': resolutionNotes,
      'resolvedAt': resolvedAt?.toIso8601String(),
      'createdAt': createdAt.toIso8601String(),
      'updatedAt': updatedAt.toIso8601String(),
      'forwardedToDepartmentId': forwardedToDepartmentId,
      'forwardedToDepartmentName': forwardedToDepartmentName,
      'forwardedAt': forwardedAt?.toIso8601String(),
      'forwardingNotes': forwardingNotes,
    };
  }

  bool get hasPhoto => photos.isNotEmpty || (photoUrl != null && photoUrl!.isNotEmpty);

  // get all photo URLs (combines photos list and legacy photoUrl)
  List<String> get allPhotos {
    final result = <String>[];
    result.addAll(photos.where((p) => p.isNotEmpty));
    if (photoUrl != null && photoUrl!.isNotEmpty && !result.contains(photoUrl)) {
      result.add(photoUrl!);
    }
    return result;
  }

  bool get isResolved => status.toLowerCase() == 'resolved';
  bool get isForwarded => status.toLowerCase() == 'forwarded';
  bool get isRejected => status.toLowerCase() == 'rejected' || status.toLowerCase() == 'dismissed';
  bool get isInProgress => status.toLowerCase() == 'inprogress';
  bool get isUnderReview => status.toLowerCase() == 'underreview' || status.toLowerCase() == 'forwarded';

  String get typeArabic {
    // updated to match report terminology
    switch (type.toLowerCase()) {
      case 'infrastructure':
        return 'بنية تحتية';
      case 'safety':
        return 'سلامة';
      case 'cleanliness':
        return 'نظافة';
      case 'equipment':
        return 'معدات';
      case 'other':
        return 'أخرى';
      // legacy support for old values
      case 'sanitation':
      case 'sanitationissue':
        return 'نظافة'; // Legacy: Map old Sanitation to Cleanliness
      case 'waterleak':
      case 'roaddamage':
      case 'lightingissue':
      case 'electricalissue':
        return 'بنية تحتية'; // Map old values to Infrastructure
      default:
        return type;
    }
  }

  String get severityArabic {
    // updated to match report terminology
    switch (severity.toLowerCase()) {
      case 'low':
        return 'منخفضة';
      case 'medium':
        return 'متوسطة';
      case 'high':
        return 'عالية';
      case 'critical':
        return 'حرجة';
      // legacy support for old values
      case 'minor':
        return 'منخفضة'; // Legacy: Map old Minor to Low
      case 'moderate':
        return 'متوسطة';
      case 'major':
        return 'عالية'; // Legacy: Map old Major to High
      default:
        return severity;
    }
  }

  bool get isClosed => status.toLowerCase() == 'closed';

  String get statusArabic {
    // map to correct enum that match backend IssueStatus enum
    switch (status.toLowerCase()) {
      case 'new':
        return 'جديدة';
      case 'forwarded':
        return 'محولة';
      case 'inprogress':
        return 'قيد المعالجة';
      case 'resolved':
        return 'تم الحل';
      case 'closed':
        return 'مغلقة';
      case 'convertedtotask':
        return 'تم تحويله إلى مهمة';
      // legacy support for old values
      case 'reported':
        return 'جديدة';
      case 'underreview':
        return 'محولة';
      case 'dismissed':
        return 'تم الحل';
      case 'rejected':
        return 'تم الحل';
      default:
        return status;
    }
  }

  @override
  String toString() {
    return 'IssueModel(issueId: $issueId, title: $title, type: $type, severity: $severity, status: $status)';
  }
}
