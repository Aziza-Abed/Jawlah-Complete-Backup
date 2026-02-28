import '../../core/utils/date_formatter.dart';
import 'local/task_local.dart';

class TaskModel {
  final int taskId;
  final int? sourceIssueId; // set if this task was created from an issue
  final String title;
  final String description;
  final String status;
  final String priority;
  final String? taskType; // New: Type of task
  final bool requiresPhotoProof; // New: Does this task require photo proof?
  final int? estimatedDurationMinutes; // New: Estimated duration
  final String? assignedTo;
  final int? assignedToUserId;
  final int? zoneId; // Zone ID
  final String? zoneName; // Zone name for display
  final String? location;
  final double? latitude;
  final double? longitude;
  final DateTime? dueDate;
  final DateTime? completedAt;   // server time (tamper-proof)
  final DateTime? eventTime;     // device time when worker actually completed it
  final String? completionNotes;
  final String? photoUrl;
  final List<String> photos; // Multiple photos from backend
  final DateTime createdAt;
  final DateTime updatedAt;
  final int syncVersion; // Added for sync conflict resolution

  // Task location verification
  final int maxDistanceMeters;
  final int? completionDistanceMeters;
  final bool isDistanceWarning;

  // Progress tracking for multi-day tasks
  final int progressPercentage;
  final String? progressNotes;
  final DateTime? extendedDeadline;

  // Auto-rejection tracking
  final bool isAutoRejected;
  final String? rejectionReason;
  final DateTime? rejectedAt;
  final int? rejectionDistanceMeters;

  TaskModel({
    required this.taskId,
    this.sourceIssueId,
    required this.title,
    required this.description,
    required this.status,
    required this.priority,
    this.taskType,
    this.requiresPhotoProof = true,
    this.estimatedDurationMinutes,
    this.assignedTo,
    this.assignedToUserId,
    this.zoneId,
    this.zoneName,
    this.location,
    this.latitude,
    this.longitude,
    this.dueDate,
    this.completedAt,
    this.eventTime,
    this.completionNotes,
    this.photoUrl,
    List<String>? photos,
    required this.createdAt,
    required this.updatedAt,
    this.syncVersion = 1,
    this.maxDistanceMeters = 100,
    this.completionDistanceMeters,
    this.isDistanceWarning = false,
    this.progressPercentage = 0,
    this.progressNotes,
    this.extendedDeadline,
    this.isAutoRejected = false,
    this.rejectionReason,
    this.rejectedAt,
    this.rejectionDistanceMeters,
  }) : photos = photos ?? (photoUrl != null ? [photoUrl] : []);

  factory TaskModel.fromJson(Map<String, dynamic> json) {
    // parse task type enum to string
    String? parseTaskType(dynamic value) {
      if (value == null) return null;
      if (value is String) return value;
      if (value is int) {
        switch (value) {
          case 0:
            return 'GarbageCollection';
          case 1:
            return 'StreetSweeping';
          case 2:
            return 'ContainerMaintenance';
          case 3:
            return 'RepairMaintenance';
          case 4:
            return 'PublicSpaceCleaning';
          case 5:
            return 'Inspection';
          case 99:
            return 'Other';
          default:
            return null;
        }
      }
      return null;
    }

    return TaskModel(
      taskId: json['taskId'] as int? ?? json['TaskId'] as int? ?? 0,
      sourceIssueId: json['sourceIssueId'] as int? ?? json['SourceIssueId'] as int?,
      title: json['title'] as String? ?? json['Title'] as String? ?? '',
      description: json['description'] as String? ??
          json['Description'] as String? ??
          '',
      status:
          json['status'] as String? ?? json['Status'] as String? ?? 'Pending',
      priority: json['priority'] as String? ??
          json['Priority'] as String? ??
          'Medium',
      taskType: parseTaskType(json['taskType'] ?? json['TaskType']),
      requiresPhotoProof: json['requiresPhotoProof'] as bool? ??
          json['RequiresPhotoProof'] as bool? ??
          true,
      estimatedDurationMinutes: json['estimatedDurationMinutes'] as int? ??
          json['EstimatedDurationMinutes'] as int?,
      assignedTo: json['assignedTo'] as String? ??
          json['assignedToUserName'] as String?,
      assignedToUserId:
          json['assignedToUserId'] as int? ?? json['AssignedToUserId'] as int?,
      zoneId: json['zoneId'] as int? ?? json['ZoneId'] as int?,
      zoneName: json['zoneName'] as String? ?? json['ZoneName'] as String?,
      location: json['location'] as String? ??
          json['locationDescription'] as String? ??
          json['LocationDescription'] as String?,
      latitude: (json['latitude'] ?? json['Latitude']) != null
          ? ((json['latitude'] ?? json['Latitude']) as num).toDouble()
          : null,
      longitude: (json['longitude'] ?? json['Longitude']) != null
          ? ((json['longitude'] ?? json['Longitude']) as num).toDouble()
          : null,
      dueDate: DateFormatter.tryParseUtc(json['dueDate'] ?? json['DueDate']),
      completedAt:
          DateFormatter.tryParseUtc(json['completedAt'] ?? json['CompletedAt']),
      eventTime:
          DateFormatter.tryParseUtc(json['eventTime'] ?? json['EventTime']),
      completionNotes: json['completionNotes'] as String? ??
          json['CompletionNotes'] as String?,
      photoUrl: json['photoUrl'] as String? ?? json['PhotoUrl'] as String?,
      photos:
          (json['photos'] as List<dynamic>?)?.map((e) => e.toString()).toList(),
      createdAt:
          DateFormatter.tryParseUtc(json['createdAt'] ?? json['CreatedAt']) ??
              DateTime.now().toUtc(),
      updatedAt: DateFormatter.tryParseUtc(json['updatedAt'] ?? json['syncTime']) ??
          DateTime.now().toUtc(),
      syncVersion:
          json['syncVersion'] as int? ?? json['SyncVersion'] as int? ?? 1,
      maxDistanceMeters: json['maxDistanceMeters'] as int? ??
          json['MaxDistanceMeters'] as int? ??
          100,
      completionDistanceMeters: json['completionDistanceMeters'] as int? ??
          json['CompletionDistanceMeters'] as int?,
      isDistanceWarning: json['isDistanceWarning'] as bool? ??
          json['IsDistanceWarning'] as bool? ??
          false,
      progressPercentage: json['progressPercentage'] as int? ??
          json['ProgressPercentage'] as int? ??
          0,
      progressNotes:
          json['progressNotes'] as String? ?? json['ProgressNotes'] as String?,
      extendedDeadline: DateFormatter.tryParseUtc(
          json['extendedDeadline'] ?? json['ExtendedDeadline']),
      isAutoRejected: json['isAutoRejected'] as bool? ??
          json['IsAutoRejected'] as bool? ??
          false,
      rejectionReason: json['rejectionReason'] as String? ??
          json['RejectionReason'] as String?,
      rejectedAt:
          DateFormatter.tryParseUtc(json['rejectedAt'] ?? json['RejectedAt']),
      rejectionDistanceMeters: json['rejectionDistanceMeters'] as int? ??
          json['RejectionDistanceMeters'] as int?,
    );
  }

  // convert from local Hive model
  factory TaskModel.fromLocal(TaskLocal local) {
    return TaskModel(
      taskId: local.taskId,
      title: local.title ?? '',
      description: local.description ?? '',
      status: local.status,
      priority: local.priority,
      taskType: local.taskType,
      requiresPhotoProof: local.requiresPhotoProof,
      estimatedDurationMinutes: local.estimatedDurationMinutes,
      assignedTo: null, // Not stored in local model
      assignedToUserId: null, // Not stored in local model
      zoneId: local.zoneId,
      zoneName: local.zoneName,
      location: local.locationDescription,
      latitude: local.latitude,
      longitude: local.longitude,
      dueDate: local.dueDate,
      completedAt: local.completedAt,
      completionNotes: local.completionNotes,
      photoUrl: local.photoUrl,
      photos: local.photos.isNotEmpty ? local.photos : (local.photoUrl != null ? [local.photoUrl!] : []),
      createdAt: local.updatedAt, // Use updatedAt as createdAt fallback
      updatedAt: local.updatedAt,
      syncVersion: local.syncVersion,
      progressPercentage: local.progressPercentage,
      progressNotes: local.progressNotes,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'taskId': taskId,
      'title': title,
      'description': description,
      'status': status,
      'priority': priority,
      'taskType': taskType,
      'requiresPhotoProof': requiresPhotoProof,
      'estimatedDurationMinutes': estimatedDurationMinutes,
      'assignedTo': assignedTo,
      'assignedToUserId': assignedToUserId,
      'zoneId': zoneId,
      'zoneName': zoneName,
      'location': location,
      'latitude': latitude,
      'longitude': longitude,
      'dueDate': dueDate?.toIso8601String(),
      'completedAt': completedAt?.toIso8601String(),
      'eventTime': eventTime?.toIso8601String(),
      'completionNotes': completionNotes,
      'photoUrl': photoUrl,
      'createdAt': createdAt.toIso8601String(),
      'updatedAt': updatedAt.toIso8601String(),
      'syncVersion': syncVersion,
      'progressPercentage': progressPercentage,
      'progressNotes': progressNotes,
      'extendedDeadline': extendedDeadline?.toIso8601String(),
    };
  }

  TaskModel copyWith({
    int? taskId,
    int? sourceIssueId,
    String? title,
    String? description,
    String? status,
    String? priority,
    String? taskType,
    bool? requiresPhotoProof,
    int? estimatedDurationMinutes,
    String? assignedTo,
    int? assignedToUserId,
    int? zoneId,
    String? zoneName,
    String? location,
    double? latitude,
    double? longitude,
    DateTime? dueDate,
    DateTime? completedAt,
    DateTime? eventTime,
    String? completionNotes,
    String? photoUrl,
    DateTime? createdAt,
    DateTime? updatedAt,
    int? syncVersion,
    int? maxDistanceMeters,
    int? completionDistanceMeters,
    bool? isDistanceWarning,
    int? progressPercentage,
    String? progressNotes,
    DateTime? extendedDeadline,
    bool? isAutoRejected,
    String? rejectionReason,
    DateTime? rejectedAt,
    int? rejectionDistanceMeters,
  }) {
    return TaskModel(
      taskId: taskId ?? this.taskId,
      sourceIssueId: sourceIssueId ?? this.sourceIssueId,
      title: title ?? this.title,
      description: description ?? this.description,
      status: status ?? this.status,
      priority: priority ?? this.priority,
      taskType: taskType ?? this.taskType,
      requiresPhotoProof: requiresPhotoProof ?? this.requiresPhotoProof,
      estimatedDurationMinutes:
          estimatedDurationMinutes ?? this.estimatedDurationMinutes,
      assignedTo: assignedTo ?? this.assignedTo,
      assignedToUserId: assignedToUserId ?? this.assignedToUserId,
      zoneId: zoneId ?? this.zoneId,
      zoneName: zoneName ?? this.zoneName,
      location: location ?? this.location,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      dueDate: dueDate ?? this.dueDate,
      completedAt: completedAt ?? this.completedAt,
      eventTime: eventTime ?? this.eventTime,
      completionNotes: completionNotes ?? this.completionNotes,
      photoUrl: photoUrl ?? this.photoUrl,
      createdAt: createdAt ?? this.createdAt,
      updatedAt: updatedAt ?? this.updatedAt,
      syncVersion: syncVersion ?? this.syncVersion,
      maxDistanceMeters: maxDistanceMeters ?? this.maxDistanceMeters,
      completionDistanceMeters:
          completionDistanceMeters ?? this.completionDistanceMeters,
      isDistanceWarning: isDistanceWarning ?? this.isDistanceWarning,
      progressPercentage: progressPercentage ?? this.progressPercentage,
      progressNotes: progressNotes ?? this.progressNotes,
      extendedDeadline: extendedDeadline ?? this.extendedDeadline,
      isAutoRejected: isAutoRejected ?? this.isAutoRejected,
      rejectionReason: rejectionReason ?? this.rejectionReason,
      rejectedAt: rejectedAt ?? this.rejectedAt,
      rejectionDistanceMeters:
          rejectionDistanceMeters ?? this.rejectionDistanceMeters,
    );
  }

  bool get isPending => status.toLowerCase() == 'pending';
  bool get isCreated => status.toLowerCase() == 'created';
  bool get isAssigned => status.toLowerCase() == 'assigned';
  bool get isAccepted => status.toLowerCase() == 'accepted';

  bool get isInProgress => status.toLowerCase() == 'inprogress';
  bool get isSubmitted => status.toLowerCase() == 'submitted';

  bool get isUnderReview => status.toLowerCase() == 'underreview';

  bool get isCompleted => status.toLowerCase() == 'completed';
  bool get isSynced => status.toLowerCase() == 'synced';
  bool get isRejected => status.toLowerCase() == 'rejected';
  bool get isCancelled => status.toLowerCase() == 'cancelled';
  bool get isFailedSync => status.toLowerCase() == 'failedsync';

  bool get isOverdue {
    if (dueDate == null || isUnderReview || isCompleted) return false;
    return DateTime.now().toUtc().isAfter(dueDate!);
  }

  bool get hasLocation => latitude != null && longitude != null;

  bool get hasPhoto =>
      photos.isNotEmpty || (photoUrl != null && photoUrl!.isNotEmpty);

  String get statusArabic {
    switch (status.toLowerCase()) {
      case 'pending':
      case 'created':
        return 'جديد';
      case 'assigned':
        return 'تم التعيين';
      case 'accepted':
        return 'مقبولة';
      case 'inprogress':
        return 'قيد التنفيذ';
      case 'submitted':
      case 'underreview':
        return 'قيد المراجعة';
      case 'completed':
      case 'synced':
        return 'مكتملة';
      case 'rejected':
        return 'مرفوضة';
      case 'cancelled':
        return 'ملغاة';
      case 'failedsync':
        return 'فشل المزامنة';
      default:
        return status;
    }
  }

  String get priorityArabic {
    switch (priority.toLowerCase()) {
      case 'low':
        return 'منخفضة';
      case 'medium':
        return 'متوسطة';
      case 'high':
        return 'عالية';
      case 'urgent':
        return 'عاجلة';
      default:
        return priority;
    }
  }

  String get taskTypeArabic {
    if (taskType == null) return '';
    switch (taskType!) {
      case 'GarbageCollection':
        return 'جمع النفايات';
      case 'StreetSweeping':
        return 'كنس الشوارع';
      case 'ContainerMaintenance':
        return 'صيانة الحاويات';
      case 'RepairMaintenance':
        return 'أعمال الصيانة';
      case 'PublicSpaceCleaning':
        return 'تنظيف الأماكن العامة';
      case 'Inspection':
        return 'التفتيش';
      case 'Other':
        return 'أخرى';
      default:
        return taskType!;
    }
  }

  String get estimatedDurationFormatted {
    if (estimatedDurationMinutes == null) return '';
    final hours = estimatedDurationMinutes! ~/ 60;
    final minutes = estimatedDurationMinutes! % 60;
    if (hours > 0 && minutes > 0) {
      return '$hours ساعة و $minutes دقيقة';
    } else if (hours > 0) {
      return '$hours ساعة';
    } else {
      return '$minutes دقيقة';
    }
  }

  @override
  String toString() {
    return 'TaskModel(taskId: $taskId, title: $title, status: $status, priority: $priority)';
  }

  TaskLocal toLocal() {
    return TaskLocal(
      taskId: taskId,
      title: title,
      description: description,
      status: status,
      priority: priority,
      taskType: taskType,
      requiresPhotoProof: requiresPhotoProof,
      estimatedDurationMinutes: estimatedDurationMinutes,
      completionNotes: completionNotes,
      photoUrl: photoUrl,
      photos: photos,
      completedAt: completedAt,
      eventTime: eventTime,
      updatedAt: updatedAt,
      dueDate: dueDate,
      zoneId: zoneId,
      zoneName: zoneName,
      latitude: latitude,
      longitude: longitude,
      locationDescription: location,
      isSynced: true, // Default to true if converting from API model
      syncVersion: syncVersion,
      progressPercentage: progressPercentage,
      progressNotes: progressNotes,
    );
  }
}
