class IssueModel {
  final int issueId;
  final String title;
  final String description;
  final String type; // Type of issue (e.g., "Equipment", "Road")
  final String severity; // How serious is it? (e.g., "High", "Low")
  final String status; // Current status (e.g., "Reported", "Resolved")
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
  });

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
          json['status'] as String? ?? json['Status'] as String? ?? 'Reported',
      reportedByUserId:
          json['reportedByUserId'] as int? ?? json['ReportedByUserId'] as int? ?? 0,
      reportedByName: json['reportedByName'] as String? ??
          json['ReportedByName'] as String?,
      location:
          json['location'] as String? ?? json['LocationDescription'] as String?,

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
      createdAt: (json['createdAt'] ?? json['ReportedAt']) != null
          ? DateTime.parse(((json['createdAt'] ?? json['ReportedAt']) as String)
                  .endsWith('Z')
              ? (json['createdAt'] ?? json['ReportedAt']) as String
              : '${(json['createdAt'] ?? json['ReportedAt'])}Z')
          : DateTime.now().toUtc(),
      updatedAt: (json['updatedAt'] ?? json['SyncTime']) != null
          ? DateTime.parse(
              ((json['updatedAt'] ?? json['SyncTime']) as String).endsWith('Z')
                  ? (json['updatedAt'] ?? json['SyncTime']) as String
                  : '${(json['updatedAt'] ?? json['SyncTime'])}Z')
          : DateTime.now().toUtc(),
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

  bool get isDismissed => status.toLowerCase() == 'dismissed';
  
  // legacy support
  bool get isRejected => status.toLowerCase() == 'rejected' || status.toLowerCase() == 'dismissed';

  bool get isInProgress => status.toLowerCase() == 'inprogress';

  bool get isUnderReview => status.toLowerCase() == 'underreview';

  String get typeArabic {
    // map to correct enum that match backend IssueType enum
    switch (type.toLowerCase()) {
      case 'infrastructure':
        return 'بنية تحتية';
      case 'safety':
        return 'سلامة';
      case 'sanitation':
        return 'نظافة';
      case 'equipment':
        return 'معدات';
      case 'other':
        return 'أخرى';
      // legacy support for old values (if any exist in database)
      case 'waterleak':
      case 'roaddamage':
      case 'lightingissue':
      case 'electricalissue':
        return 'بنية تحتية'; // Map old values to Infrastructure
      case 'sanitationissue':
        return 'نظافة'; // Map to Sanitation
      default:
        return type;
    }
  }

  String get severityArabic {
    // map to correct enum that match backend IssueSeverity enum
    switch (severity.toLowerCase()) {
      case 'minor':
        return 'بسيطة';
      case 'moderate':
        return 'متوسطة';
      case 'major':
        return 'كبيرة';
      case 'critical':
        return 'حرجة';
      // legacy support for old values
      case 'low':
        return 'بسيطة'; // Map to Minor
      case 'medium':
        return 'متوسطة';
      case 'high':
        return 'كبيرة'; // Map to Major
      default:
        return severity;
    }
  }

  String get statusArabic {
    // map to correct enum that match backend IssueStatus enum
    switch (status.toLowerCase()) {
      case 'reported':
        return 'تم التبليغ';
      case 'underreview':
        return 'قيد المراجعة';
      case 'resolved':
        return 'تم الحل';
      case 'dismissed':
        return 'مرفوضة';
      // legacy support for old values
      case 'inprogress':
        return 'قيد المعالجة'; // Map to UnderReview if needed
      case 'rejected':
        return 'مرفوضة'; // Map to Dismissed
      default:
        return status;
    }
  }

  @override
  String toString() {
    return 'IssueModel(issueId: $issueId, title: $title, type: $type, severity: $severity, status: $status)';
  }
}
