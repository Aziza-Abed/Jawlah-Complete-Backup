class UserModel {
  final int userId;
  final String employeeId;
  final String fullName;
  final String phoneNumber;
  final String role;
  final String? workerType;
  final String? email;
  final DateTime createdAt;

  // Privacy consent tracking
  final bool hasConsented;
  final DateTime? privacyConsentedAt;
  final int consentVersion;

  // Current required consent version - must match backend
  static const int requiredConsentVersion = 1;

  UserModel({
    required this.userId,
    required this.employeeId,
    required this.fullName,
    required this.phoneNumber,
    required this.role,
    this.workerType,
    this.email,
    required this.createdAt,
    this.hasConsented = false,
    this.privacyConsentedAt,
    this.consentVersion = 0,
  });

  // Helper to check if user needs to consent
  bool get needsConsent => consentVersion < requiredConsentVersion;

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
      // use current date if createdAt is missing
      // parse as UTC
      createdAt: json['createdAt'] != null
          ? DateTime.parse('${json['createdAt'] as String}Z')
          : DateTime.now().toUtc(),
      // Privacy consent fields
      hasConsented: json['hasConsented'] as bool? ?? false,
      privacyConsentedAt: json['privacyConsentedAt'] != null
          ? DateTime.tryParse('${json['privacyConsentedAt']}Z')
          : null,
      consentVersion: json['consentVersion'] as int? ?? 0,
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
      'hasConsented': hasConsented,
      'privacyConsentedAt': privacyConsentedAt?.toIso8601String(),
      'consentVersion': consentVersion,
    };
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
    bool? hasConsented,
    DateTime? privacyConsentedAt,
    int? consentVersion,
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
      hasConsented: hasConsented ?? this.hasConsented,
      privacyConsentedAt: privacyConsentedAt ?? this.privacyConsentedAt,
      consentVersion: consentVersion ?? this.consentVersion,
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
    switch (workerType!.toLowerCase()) {
      case 'sanitation':
      case '0':
        return 'عامل نظافة';
      case 'inspector':
      case '1':
        return 'مفتش';
      case 'emergency':
      case '2':
        return 'طوارئ';
      case 'maintenance':
      case '3':
        return 'عامل صيانة';
      default:
        return workerType!;
    }
  }

  @override
  String toString() {
    return 'UserModel(userId: $userId, employeeId: $employeeId, fullName: $fullName, role: $role)';
  }
}
