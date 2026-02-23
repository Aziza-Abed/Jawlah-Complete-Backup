class UserModel {
  final int userId;
  final String employeeId;
  final String fullName;
  final String phoneNumber;
  final String role;
  final String? workerType;
  final String? email;
  final DateTime createdAt;

  UserModel({
    required this.userId,
    required this.employeeId,
    required this.fullName,
    required this.phoneNumber,
    required this.role,
    this.workerType,
    this.email,
    required this.createdAt,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      userId: json['userId'] as int,
      // try employeeId, pin, or username
      employeeId: (json['employeeId'] ?? json['pin'] ?? json['username'] ?? '')
          as String,
      fullName: json['fullName'] as String,
      // use empty string if phone number is missing
      phoneNumber: (json['phoneNumber'] ?? '') as String,
      role: json['role'] as String,
      workerType: json['workerType'] as String?,
      email: json['email'] as String?,
      // use current date if createdAt is missing or malformed
      // parse as UTC with error handling
      createdAt: _parseDateTime(json['createdAt']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'userId': userId,
      'employeeId': employeeId,
      'fullName': fullName,
      'phoneNumber': phoneNumber,
      'role': role,
      'workerType': workerType,
      'email': email,
      'createdAt': createdAt.toIso8601String(),
    };
  }

  // Safe DateTime parsing to prevent crashes from malformed dates
  static DateTime _parseDateTime(dynamic dateValue) {
    if (dateValue == null) {
      return DateTime.now().toUtc();
    }

    try {
      final dateStr = dateValue as String;
      // Add Z suffix if not already present to parse as UTC
      final normalizedDate = dateStr.endsWith('Z') ? dateStr : '${dateStr}Z';
      return DateTime.parse(normalizedDate);
    } catch (e) {
      // If parsing fails, use current time instead of crashing
      return DateTime.now().toUtc();
    }
  }

  UserModel copyWith({
    int? userId,
    String? employeeId,
    String? fullName,
    String? phoneNumber,
    String? role,
    String? workerType,
    String? email,
    DateTime? createdAt,
  }) {
    return UserModel(
      userId: userId ?? this.userId,
      employeeId: employeeId ?? this.employeeId,
      fullName: fullName ?? this.fullName,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      role: role ?? this.role,
      workerType: workerType ?? this.workerType,
      email: email ?? this.email,
      createdAt: createdAt ?? this.createdAt,
    );
  }

  bool get isWorker => role.toLowerCase() == 'worker';

  bool get isSupervisor => role.toLowerCase() == 'supervisor';

  bool get isAdmin => role.toLowerCase() == 'admin';

  String get roleArabic {
    switch (role.toLowerCase()) {
      case 'worker':
        return 'عامل';
      case 'supervisor':
        return 'مشرف';
      case 'admin':
        return 'مدير';
      default:
        return role;
    }
  }

  String get workerTypeArabic {
    if (workerType == null || workerType!.isEmpty) return '';

    // Handle both enum names and numeric values
    // Standard municipality department worker types
    switch (workerType!.toLowerCase()) {
      case 'sanitation':
      case '0':
        return 'صحة/نظافة';
      case 'publicworks':
      case '1':
        return 'أشغال';
      case 'agriculture':
      case '2':
        return 'زراعة';
      case 'maintenance':
      case '3':
        return 'صيانة';
      // Legacy support for old values
      case 'inspector':
        return 'مفتش';
      case 'emergency':
        return 'طوارئ';
      default:
        return workerType!;
    }
  }

  @override
  String toString() {
    return 'UserModel(userId: $userId, employeeId: $employeeId, fullName: $fullName, role: $role)';
  }
}
