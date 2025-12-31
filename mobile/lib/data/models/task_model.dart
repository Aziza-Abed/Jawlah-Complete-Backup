import 'local/task_local.dart';

class TaskModel {
  final int taskId;
  final String title;
  final String description;
  final String status;
  final String priority;
  final String? taskType; // New: Type of task
  final bool requiresPhotoProof; // New: Does this task require photo proof?
  final int? estimatedDurationMinutes; // New: Estimated duration
  final String? assignedTo;
  final int? assignedToUserId;
  final String? location;
  final double? latitude;
  final double? longitude;
  final DateTime? dueDate;
  final DateTime? completedAt;
  final String? completionNotes;
  final String? photoUrl;
  final List<String> photos; // Multiple photos from backend
  final DateTime createdAt;
  final DateTime updatedAt;
  final int syncVersion; // Added for sync conflict resolution

  TaskModel({
    required this.taskId,
    required this.title,
    required this.description,
    required this.status,
    required this.priority,
    this.taskType,
    this.requiresPhotoProof = true,
    this.estimatedDurationMinutes,
    this.assignedTo,
    this.assignedToUserId,
    this.location,
    this.latitude,
    this.longitude,
    this.dueDate,
    this.completedAt,
    this.completionNotes,
    this.photoUrl,
    List<String>? photos,
    required this.createdAt,
    required this.updatedAt,
    this.syncVersion = 1,
  }) : photos = photos ?? (photoUrl != null ? [photoUrl] : []);

  factory TaskModel.fromJson(Map<String, dynamic> json) {
    // parse task type enum to string
    String? parseTaskType(dynamic value) {
      if (value == null) return null;
      if (value is String) return value;
      if (value is int) {
        switch (value) {
          case 0: return 'GarbageCollection';
          case 1: return 'StreetSweeping';
          case 2: return 'ContainerMaintenance';
          case 3: return 'RepairMaintenance';
          case 4: return 'PublicSpaceCleaning';
          case 5: return 'Inspection';
          case 99: return 'Other';
          default: return null;
        }
      }
      return null;
    }

    return TaskModel(
      taskId: json['taskId'] as int? ?? json['TaskId'] as int? ?? 0,
      title: json['title'] as String? ?? json['Title'] as String? ?? '',
      description:
          json['description'] as String? ?? json['Description'] as String? ?? '',
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
          json['AssignedToUserName'] as String?,
      assignedToUserId:
          json['assignedToUserId'] as int? ?? json['AssignedToUserId'] as int?,
      location:
          json['location'] as String? ??
          json['locationDescription'] as String? ??
          json['LocationDescription'] as String?,
      latitude: (json['latitude'] ?? json['Latitude']) != null
          ? ((json['latitude'] ?? json['Latitude']) as num).toDouble()
          : null,
      longitude: (json['longitude'] ?? json['Longitude']) != null
          ? ((json['longitude'] ?? json['Longitude']) as num).toDouble()
          : null,
      dueDate: (json['dueDate'] ?? json['DueDate']) != null
          ? DateTime.parse(
              ((json['dueDate'] ?? json['DueDate']) as String).endsWith('Z')
                  ? (json['dueDate'] ?? json['DueDate']) as String
                  : '${(json['dueDate'] ?? json['DueDate'])}Z')
          : null,
      completedAt: (json['completedAt'] ?? json['CompletedAt']) != null
          ? DateTime.parse(
              ((json['completedAt'] ?? json['CompletedAt']) as String)
                      .endsWith('Z')
                  ? (json['completedAt'] ?? json['CompletedAt']) as String
                  : '${(json['completedAt'] ?? json['CompletedAt'])}Z')
          : null,
      completionNotes: json['completionNotes'] as String? ??
          json['CompletionNotes'] as String?,
      photoUrl: json['photoUrl'] as String? ?? json['PhotoUrl'] as String?,
      photos: (json['photos'] as List<dynamic>?)
          ?.map((e) => e.toString())
          .toList(),
      createdAt: (json['createdAt'] ?? json['CreatedAt']) != null
          ? DateTime.parse(
              ((json['createdAt'] ?? json['CreatedAt']) as String).endsWith('Z')
                  ? (json['createdAt'] ?? json['CreatedAt']) as String
                  : '${(json['createdAt'] ?? json['CreatedAt'])}Z')
          : DateTime.now().toUtc(),
      updatedAt: (json['updatedAt'] ?? json['SyncTime']) != null
          ? DateTime.parse(
              ((json['updatedAt'] ?? json['SyncTime']) as String).endsWith('Z')
                  ? (json['updatedAt'] ?? json['SyncTime']) as String
                  : '${(json['updatedAt'] ?? json['SyncTime'])}Z')
          : DateTime.now().toUtc(),
      syncVersion:
          json['syncVersion'] as int? ?? json['SyncVersion'] as int? ?? 1,
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
      location: local.locationDescription,
      latitude: local.latitude,
      longitude: local.longitude,
      dueDate: local.dueDate,
      completedAt: local.completedAt,
      completionNotes: local.completionNotes,
      photoUrl: local.photoUrl,
      photos: local.photoUrl != null ? [local.photoUrl!] : [],
      createdAt: local.updatedAt, // Use updatedAt as createdAt fallback
      updatedAt: local.updatedAt,
      syncVersion: local.syncVersion,
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
      'location': location,
      'latitude': latitude,
      'longitude': longitude,
      'dueDate': dueDate?.toIso8601String(),
      'completedAt': completedAt?.toIso8601String(),
      'completionNotes': completionNotes,
      'photoUrl': photoUrl,
      'createdAt': createdAt.toIso8601String(),
      'updatedAt': updatedAt.toIso8601String(),
      'syncVersion': syncVersion,
    };
  }

  TaskModel copyWith({
    int? taskId,
    String? title,
    String? description,
    String? status,
    String? priority,
    String? taskType,
    bool? requiresPhotoProof,
    int? estimatedDurationMinutes,
    String? assignedTo,
    int? assignedToUserId,
    String? location,
    double? latitude,
    double? longitude,
    DateTime? dueDate,
    DateTime? completedAt,
    String? completionNotes,
    String? photoUrl,
    DateTime? createdAt,
    DateTime? updatedAt,
    int? syncVersion,
  }) {
    return TaskModel(
      taskId: taskId ?? this.taskId,
      title: title ?? this.title,
      description: description ?? this.description,
      status: status ?? this.status,
      priority: priority ?? this.priority,
      taskType: taskType ?? this.taskType,
      requiresPhotoProof: requiresPhotoProof ?? this.requiresPhotoProof,
      estimatedDurationMinutes: estimatedDurationMinutes ?? this.estimatedDurationMinutes,
      assignedTo: assignedTo ?? this.assignedTo,
      assignedToUserId: assignedToUserId ?? this.assignedToUserId,
      location: location ?? this.location,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      dueDate: dueDate ?? this.dueDate,
      completedAt: completedAt ?? this.completedAt,
      completionNotes: completionNotes ?? this.completionNotes,
      photoUrl: photoUrl ?? this.photoUrl,
      createdAt: createdAt ?? this.createdAt,
      updatedAt: updatedAt ?? this.updatedAt,
      syncVersion: syncVersion ?? this.syncVersion,
    );
  }

  bool get isPending => status.toLowerCase() == 'pending';

  bool get isInProgress => status.toLowerCase() == 'inprogress';

  bool get isCompleted => status.toLowerCase() == 'completed';

  // note: isCancelled removed - workers cannot cancel tasks per project decision
  // supervisors can set Approved/Rejected status instead
  bool get isApproved => status.toLowerCase() == 'approved';
  bool get isRejected => status.toLowerCase() == 'rejected';

  bool get isOverdue {
    if (dueDate == null || isCompleted) return false;
    return DateTime.now().isAfter(dueDate!);
  }

  bool get hasLocation => latitude != null && longitude != null;

  bool get hasPhoto => photos.isNotEmpty || (photoUrl != null && photoUrl!.isNotEmpty);

  String get statusArabic {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'جديد';
      case 'inprogress':
        return 'قيد التنفيذ';
      case 'completed':
        return 'مكتملة';
      case 'approved':
        return 'معتمدة';
      case 'rejected':
        return 'مرفوضة';
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
      completedAt: completedAt,
      updatedAt: updatedAt,
      dueDate: dueDate,
      latitude: latitude,
      longitude: longitude,
      locationDescription: location,
      isSynced: true, // Default to true if converting from API model
      syncVersion: syncVersion,
    );
  }
}
