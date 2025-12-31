class WorkerLocation {
  final int userId;
  final String fullName;
  final String role;
  final double latitude;
  final double longitude;
  final DateTime? lastUpdate;
  final double? speed;
  final double? accuracy;

  WorkerLocation({
    required this.userId,
    required this.fullName,
    required this.role,
    required this.latitude,
    required this.longitude,
    this.lastUpdate,
    this.speed,
    this.accuracy,
  });

  factory WorkerLocation.fromJson(Map<String, dynamic> json) {
    return WorkerLocation(
      userId: json['userId'] as int,
      fullName: json['fullName'] as String? ?? 'Unknown Worker',
      role: json['role'] as String? ?? 'Worker',
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      lastUpdate: json['lastUpdate'] != null
          ? DateTime.parse(json['lastUpdate'] as String)
          : null,
      speed: json['speed'] != null ? (json['speed'] as num).toDouble() : null,
      accuracy: json['accuracy'] != null
          ? (json['accuracy'] as num).toDouble()
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'userId': userId,
      'fullName': fullName,
      'role': role,
      'latitude': latitude,
      'longitude': longitude,
      'lastUpdate': lastUpdate?.toIso8601String(),
      'speed': speed,
      'accuracy': accuracy,
    };
  }

  WorkerLocation copyWith({
    int? userId,
    String? fullName,
    String? role,
    double? latitude,
    double? longitude,
    DateTime? lastUpdate,
    double? speed,
    double? accuracy,
  }) {
    return WorkerLocation(
      userId: userId ?? this.userId,
      fullName: fullName ?? this.fullName,
      role: role ?? this.role,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      lastUpdate: lastUpdate ?? this.lastUpdate,
      speed: speed ?? this.speed,
      accuracy: accuracy ?? this.accuracy,
    );
  }

  bool get isMoving => speed != null && speed! > 0.5; // Moving if > 0.5 m/s
  bool get hasAccurateLocation =>
      accuracy != null && accuracy! < 50; // Accurate if < 50m

  String get statusText {
    if (!hasAccurateLocation) {
      return 'موقع غير دقيق';
    }
    if (isMoving) {
      return 'يتحرك (${speed!.toStringAsFixed(1)} م/ث)';
    }
    return 'متوقف';
  }
}
