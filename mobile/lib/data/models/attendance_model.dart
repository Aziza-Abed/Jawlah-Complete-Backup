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
  });

  factory AttendanceModel.fromJson(Map<String, dynamic> json) {
    return AttendanceModel(
      attendanceId: json['attendanceId'] as int,
      userId: json['userId'] as int,
      // backend uses 'userName'
      employeeName:
          json['userName'] as String? ?? json['employeeName'] as String?,
      // backend uses 'checkInEventTime'
      // parse as UTC for correct timezone conversion
      checkInTime: json['checkInEventTime'] != null
          ? _parseAsUtc(json['checkInEventTime'] as String)
          : (json['checkInTime'] != null
              ? _parseAsUtc(json['checkInTime'] as String)
              : DateTime.now().toUtc()),
      // backend uses 'checkOutEventTime'
      checkOutTime: json['checkOutEventTime'] != null
          ? _parseAsUtc(json['checkOutEventTime'] as String)
          : (json['checkOutTime'] != null
              ? _parseAsUtc(json['checkOutTime'] as String)
              : null),
      checkInLatitude: (json['checkInLatitude'] as num).toDouble(),
      checkInLongitude: (json['checkInLongitude'] as num).toDouble(),
      checkOutLatitude: json['checkOutLatitude'] != null
          ? (json['checkOutLatitude'] as num).toDouble()
          : null,
      checkOutLongitude: json['checkOutLongitude'] != null
          ? (json['checkOutLongitude'] as num).toDouble()
          : null,
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
          ? _parseAsUtc(json['createdAt'] as String)
          : DateTime.now().toUtc(),
    );
  }

  // helper to parse DateTime string as UTC regardless of format
  static DateTime _parseAsUtc(String dateStr) {
    // if already has Z suffix, parse normally
    if (dateStr.endsWith('Z')) {
      return DateTime.parse(dateStr);
    }
    // otherwise, add Z to force UTC parsing
    return DateTime.parse('${dateStr}Z');
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
    };
  }

  AttendanceModel copyWith({
    int? attendanceId,
    int? userId,
    String? employeeName,
    DateTime? checkInTime,
    DateTime? checkOutTime,
    double? checkInLatitude,
    double? checkInLongitude,
    double? checkOutLatitude,
    double? checkOutLongitude,
    String? checkInZoneName,
    String? checkOutZoneName,
    bool? isValid,
    String? notes,
    DateTime? createdAt,
  }) {
    return AttendanceModel(
      attendanceId: attendanceId ?? this.attendanceId,
      userId: userId ?? this.userId,
      employeeName: employeeName ?? this.employeeName,
      checkInTime: checkInTime ?? this.checkInTime,
      checkOutTime: checkOutTime ?? this.checkOutTime,
      checkInLatitude: checkInLatitude ?? this.checkInLatitude,
      checkInLongitude: checkInLongitude ?? this.checkInLongitude,
      checkOutLatitude: checkOutLatitude ?? this.checkOutLatitude,
      checkOutLongitude: checkOutLongitude ?? this.checkOutLongitude,
      checkInZoneName: checkInZoneName ?? this.checkInZoneName,
      checkOutZoneName: checkOutZoneName ?? this.checkOutZoneName,
      isValid: isValid ?? this.isValid,
      notes: notes ?? this.notes,
      createdAt: createdAt ?? this.createdAt,
    );
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

  String get checkInTimeFormatted {
    // convert to local time time before displaying
    final localTime = checkInTime.toLocal();
    final hour = localTime.hour;
    final minute = localTime.minute.toString().padLeft(2, '0');
    final period = hour < 12 ? 'ص' : 'م';
    final displayHour = hour > 12 ? hour - 12 : (hour == 0 ? 12 : hour);
    return '$displayHour:$minute $period';
  }

  String? get checkOutTimeFormatted {
    if (checkOutTime == null) return null;

    // convert to local time time before displaying
    final localTime = checkOutTime!.toLocal();
    final hour = localTime.hour;
    final minute = localTime.minute.toString().padLeft(2, '0');
    final period = hour < 12 ? 'ص' : 'م';
    final displayHour = hour > 12 ? hour - 12 : (hour == 0 ? 12 : hour);
    return '$displayHour:$minute $period';
  }

  @override
  String toString() {
    return 'AttendanceModel(attendanceId: $attendanceId, userId: $userId, checkInTime: $checkInTime, isActive: $isActive)';
  }
}
