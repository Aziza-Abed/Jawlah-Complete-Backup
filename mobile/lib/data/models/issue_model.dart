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

  // Issue forwarding info (SR15)
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
      resolvedAt: (json['resolvedAt'] ?? json['ResolvedAt']) != null
          ? DateTime.parse(
              ((json['resolvedAt'] ?? json['ResolvedAt']) as String)
                      .endsWith('Z')
                  ? (json['resolvedAt'] ?? json['ResolvedAt']) as String
                  : '${(json['resolvedAt'] ?? json['ResolvedAt'])}Z')
          : null,
      createdAt: (json['createdAt'] ?? json['reportedAt']) != null
          ? DateTime.parse(((json['createdAt'] ?? json['reportedAt']) as String)
                  .endsWith('Z')
              ? (json['createdAt'] ?? json['reportedAt']) as String
              : '${(json['createdAt'] ?? json['reportedAt'])}Z')
          : DateTime.now().toUtc(),
      updatedAt: (json['updatedAt'] ?? json['syncTime']) != null
          ? DateTime.parse(
              ((json['updatedAt'] ?? json['syncTime']) as String).endsWith('Z')
                  ? (json['updatedAt'] ?? json['syncTime']) as String
                  : '${(json['updatedAt'] ?? json['syncTime'])}Z')
          : DateTime.now().toUtc(),
      forwardedToDepartmentId: json['forwardedToDepartmentId'] as int?,
      forwardedToDepartmentName: json['forwardedToDepartmentName'] as String?,
      forwardedAt: json['forwardedAt'] != null
          ? DateTime.tryParse(json['forwardedAt'] as String)
          : null,
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

  IssueModel copyWith({
    int? issueId,
    String? title,
    String? description,
    String? type,
    String? severity,
    String? status,
    int? reportedByUserId,
    String? reportedByName,
    String? location,
    double? latitude,
    double? longitude,
    String? photoUrl,
    List<String>? photos,
    String? resolutionNotes,
    DateTime? resolvedAt,
    DateTime? createdAt,
    DateTime? updatedAt,
    int? forwardedToDepartmentId,
    String? forwardedToDepartmentName,
    DateTime? forwardedAt,
    String? forwardingNotes,
  }) {
    return IssueModel(
      issueId: issueId ?? this.issueId,
      title: title ?? this.title,
      description: description ?? this.description,
      type: type ?? this.type,
      severity: severity ?? this.severity,
      status: status ?? this.status,
      reportedByUserId: reportedByUserId ?? this.reportedByUserId,
      reportedByName: reportedByName ?? this.reportedByName,
      location: location ?? this.location,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      photoUrl: photoUrl ?? this.photoUrl,
      photos: photos ?? this.photos,
      resolutionNotes: resolutionNotes ?? this.resolutionNotes,
      resolvedAt: resolvedAt ?? this.resolvedAt,
      createdAt: createdAt ?? this.createdAt,
      updatedAt: updatedAt ?? this.updatedAt,
      forwardedToDepartmentId: forwardedToDepartmentId ?? this.forwardedToDepartmentId,
      forwardedToDepartmentName: forwardedToDepartmentName ?? this.forwardedToDepartmentName,
      forwardedAt: forwardedAt ?? this.forwardedAt,
      forwardingNotes: forwardingNotes ?? this.forwardingNotes,
    );
  }

  bool get hasPhoto => photos.isNotEmpty || (photoUrl != null && photoUrl!.isNotEmpty);

  /// Get all photo URLs (combines photos list and legacy photoUrl)
  List<String> get allPhotos {
    final result = <String>[];
    result.addAll(photos);
    if (photoUrl != null && photoUrl!.isNotEmpty && !photos.contains(photoUrl)) {
      result.add(photoUrl!);
    }
    return result;
  }

  bool get isResolved => status.toLowerCase() == 'resolved';

  bool get isNew => status.toLowerCase() == 'new';

  bool get isForwarded => status.toLowerCase() == 'forwarded';

  // legacy support
  bool get isDismissed => status.toLowerCase() == 'dismissed';
  bool get isRejected => status.toLowerCase() == 'rejected' || status.toLowerCase() == 'dismissed';
  bool get isInProgress => status.toLowerCase() == 'inprogress';
  bool get isUnderReview => status.toLowerCase() == 'underreview' || status.toLowerCase() == 'forwarded';

  String get typeArabic {
    // Chapter 4: Updated to match report terminology
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
    // Chapter 4: Updated to match report terminology
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

  String get statusArabic {
    // map to correct enum that match backend IssueStatus enum
    switch (status.toLowerCase()) {
      case 'new':
        return 'جديدة';
      case 'forwarded':
        return 'محولة';
      case 'resolved':
        return 'تم الحل';
      // legacy support for old values
      case 'reported':
        return 'جديدة';
      case 'underreview':
        return 'محولة';
      case 'dismissed':
        return 'تم الحل';
      case 'inprogress':
        return 'محولة';
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
