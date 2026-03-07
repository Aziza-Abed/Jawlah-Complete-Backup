import '../../core/utils/date_formatter.dart';

class AttendanceModel {
  final int attendanceId;
  final int userId;
  final String? employeeName;
  final DateTime checkInTime;
  final DateTime? checkOutTime;
  final double checkInLatitude;
  final double checkInLongitude;
  final double? checkOutLatitude;
  final double? checkOutLongitude;
  final String? checkInZoneName;
  final String? checkOutZoneName;
  final bool isValid;
  final String? notes;
  final DateTime createdAt;

  // Lateness and overtime tracking
  final int lateMinutes;
  final int earlyLeaveMinutes;
  final int overtimeMinutes;
  final String attendanceType; // OnTime, Late, EarlyLeave, Overtime, Manual

  // Manual/GPS failure handling
  final bool isManual;
  final String? manualReason;
  final String approvalStatus; // AutoApproved, Pending, Approved, Rejected

  AttendanceModel({
    required this.attendanceId,
    required this.userId,
    this.employeeName,
    required this.checkInTime,
    this.checkOutTime,
    required this.checkInLatitude,
    required this.checkInLongitude,
    this.checkOutLatitude,
    this.checkOutLongitude,
    this.checkInZoneName,
    this.checkOutZoneName,
    required this.isValid,
    this.notes,
    required this.createdAt,
    this.lateMinutes = 0,
    this.earlyLeaveMinutes = 0,
    this.overtimeMinutes = 0,
    this.attendanceType = 'OnTime',
    this.isManual = false,
    this.manualReason,
    this.approvalStatus = 'AutoApproved',
  });

  factory AttendanceModel.fromJson(Map<String, dynamic> json) {
    return AttendanceModel(
      attendanceId: (json['attendanceId'] as num?)?.toInt() ?? 0,
      userId: (json['userId'] as num?)?.toInt() ?? 0,
      // backend uses 'userName'
      employeeName:
          json['userName'] as String? ?? json['employeeName'] as String?,
      // backend uses 'checkInEventTime'
      // parse as UTC for correct timezone conversion
      checkInTime: json['checkInEventTime'] != null
          ? DateFormatter.parseUtc(json['checkInEventTime'] as String)
          : (json['checkInTime'] != null
              ? DateFormatter.parseUtc(json['checkInTime'] as String)
              : DateTime.now().toUtc()),
      // backend uses 'checkOutEventTime'
      checkOutTime: json['checkOutEventTime'] != null
          ? DateFormatter.parseUtc(json['checkOutEventTime'] as String)
          : (json['checkOutTime'] != null
              ? DateFormatter.parseUtc(json['checkOutTime'] as String)
              : null),
      checkInLatitude: (json['checkInLatitude'] as num?)?.toDouble() ?? 0.0,
      checkInLongitude: (json['checkInLongitude'] as num?)?.toDouble() ?? 0.0,
      checkOutLatitude: (json['checkOutLatitude'] as num?)?.toDouble(),
      checkOutLongitude: (json['checkOutLongitude'] as num?)?.toDouble(),
      // backend uses 'zoneName'
      checkInZoneName:
          json['zoneName'] as String? ?? json['checkInZoneName'] as String?,
      checkOutZoneName: json['checkOutZoneName'] as String?,
      // backend uses 'isValidated'
      isValid:
          json['isValidated'] as bool? ?? json['isValid'] as bool? ?? false,
      // backend uses 'validationMessage'
      notes: json['validationMessage'] as String? ?? json['notes'] as String?,
      createdAt: json['createdAt'] != null
          ? DateFormatter.parseUtc(json['createdAt'] as String)
          : DateTime.now().toUtc(),
      // Lateness tracking
      lateMinutes: json['lateMinutes'] as int? ?? 0,
      earlyLeaveMinutes: json['earlyLeaveMinutes'] as int? ?? 0,
      overtimeMinutes: json['overtimeMinutes'] as int? ?? 0,
      attendanceType: json['attendanceType'] as String? ?? 'OnTime',
      // Manual check-in
      isManual: json['isManual'] as bool? ?? false,
      manualReason: json['manualReason'] as String?,
      approvalStatus: json['approvalStatus'] as String? ?? 'AutoApproved',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'attendanceId': attendanceId,
      'userId': userId,
      'employeeName': employeeName,
      'checkInTime': checkInTime.toIso8601String(),
      'checkOutTime': checkOutTime?.toIso8601String(),
      'checkInLatitude': checkInLatitude,
      'checkInLongitude': checkInLongitude,
      'checkOutLatitude': checkOutLatitude,
      'checkOutLongitude': checkOutLongitude,
      'checkInZoneName': checkInZoneName,
      'checkOutZoneName': checkOutZoneName,
      'isValid': isValid,
      'notes': notes,
      'createdAt': createdAt.toIso8601String(),
      'lateMinutes': lateMinutes,
      'earlyLeaveMinutes': earlyLeaveMinutes,
      'overtimeMinutes': overtimeMinutes,
      'attendanceType': attendanceType,
      'isManual': isManual,
      'manualReason': manualReason,
      'approvalStatus': approvalStatus,
    };
  }

  bool get isActive => checkOutTime == null;

  Duration? get workDuration {
    if (checkOutTime == null) {
      // use UTC time to calculate correct duration
      return DateTime.now().toUtc().difference(checkInTime);
    }
    return checkOutTime!.difference(checkInTime);
  }

  String get workDurationFormatted {
    final duration = workDuration;
    if (duration == null) return '';

    final hours = duration.inHours;
    final minutes = duration.inMinutes.remainder(60);

    if (hours == 0) {
      return '$minutes دقيقة';
    } else if (minutes == 0) {
      return '$hours ساعات';
    } else {
      return '$hours ساعات $minutes دقيقة';
    }
  }

  String get checkInTimeFormatted => DateFormatter.formatTime12h(checkInTime);

  String? get checkOutTimeFormatted =>
      checkOutTime != null ? DateFormatter.formatTime12h(checkOutTime!) : null;

  @override
  String toString() {
    return 'AttendanceModel(attendanceId: $attendanceId, userId: $userId, checkInTime: $checkInTime, isActive: $isActive)';
  }
}
