class NotificationModel {
  final int notificationId;
  final String title;
  final String message;
  final String type;
  bool isRead;
  final bool isSent;
  final DateTime createdAt;
  final DateTime? sentAt;
  DateTime? readAt;

  NotificationModel({
    required this.notificationId,
    required this.title,
    required this.message,
    required this.type,
    this.isRead = false,
    this.isSent = false,
    required this.createdAt,
    this.sentAt,
    this.readAt,
  });

  factory NotificationModel.fromJson(Map<String, dynamic> json) {
    return NotificationModel(
      notificationId: json['notificationId'] as int,
      title: json['title'] as String,
      message: json['message'] as String,
      type: json['type'] as String,
      isRead: json['isRead'] as bool? ?? false,
      isSent: json['isSent'] as bool? ?? false,
      createdAt: DateTime.parse(json['createdAt'] as String),
      sentAt: json['sentAt'] != null
          ? DateTime.parse(json['sentAt'] as String)
          : null,
      readAt: json['readAt'] != null
          ? DateTime.parse(json['readAt'] as String)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'notificationId': notificationId,
      'title': title,
      'message': message,
      'type': type,
      'isRead': isRead,
      'isSent': isSent,
      'createdAt': createdAt.toIso8601String(),
      'sentAt': sentAt?.toIso8601String(),
      'readAt': readAt?.toIso8601String(),
    };
  }
}
