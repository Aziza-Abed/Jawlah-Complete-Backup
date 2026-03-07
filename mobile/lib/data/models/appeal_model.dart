import 'package:flutter/material.dart';
import '../../core/utils/date_formatter.dart';

class AppealModel {
  final int appealId;
  final String appealType; // "TaskRejection" or "AttendanceFailure"
  final String appealTypeName; // Arabic name
  final String entityType; // "Task" or "Attendance"
  final int entityId;
  final int userId;
  final String workerName;
  final String workerExplanation;
  final double? workerLatitude;
  final double? workerLongitude;
  final double? expectedLatitude;
  final double? expectedLongitude;
  final int? distanceMeters;
  final String status; // "Pending", "Approved", "Rejected"
  final String statusName; // Arabic name
  final int? reviewedByUserId;
  final String? reviewedByName;
  final DateTime? reviewedAt;
  final String? reviewNotes;
  final DateTime submittedAt;
  final String? evidencePhotoUrl;
  final String? originalRejectionReason;
  final String? entityTitle; // Task title or attendance date

  AppealModel({
    required this.appealId,
    required this.appealType,
    required this.appealTypeName,
    required this.entityType,
    required this.entityId,
    required this.userId,
    required this.workerName,
    required this.workerExplanation,
    this.workerLatitude,
    this.workerLongitude,
    this.expectedLatitude,
    this.expectedLongitude,
    this.distanceMeters,
    required this.status,
    required this.statusName,
    this.reviewedByUserId,
    this.reviewedByName,
    this.reviewedAt,
    this.reviewNotes,
    required this.submittedAt,
    this.evidencePhotoUrl,
    this.originalRejectionReason,
    this.entityTitle,
  });

  factory AppealModel.fromJson(Map<String, dynamic> json) {
    return AppealModel(
      appealId: json['appealId'] as int? ?? 0,
      appealType: json['appealType'] as String? ?? '',
      appealTypeName: json['appealTypeName'] as String? ?? '',
      entityType: json['entityType'] as String? ?? '',
      entityId: json['entityId'] as int? ?? 0,
      userId: json['userId'] as int? ?? 0,
      workerName: json['workerName'] as String? ?? '',
      workerExplanation: json['workerExplanation'] as String? ?? '',
      workerLatitude: (json['workerLatitude'] as num?)?.toDouble(),
      workerLongitude: (json['workerLongitude'] as num?)?.toDouble(),
      expectedLatitude: (json['expectedLatitude'] as num?)?.toDouble(),
      expectedLongitude: (json['expectedLongitude'] as num?)?.toDouble(),
      distanceMeters: json['distanceMeters'] as int?,
      status: json['status'] as String? ?? 'Pending',
      statusName: json['statusName'] as String? ?? '',
      reviewedByUserId: json['reviewedByUserId'] as int?,
      reviewedByName: json['reviewedByName'] as String?,
      reviewedAt: json['reviewedAt'] != null
          ? DateFormatter.parseUtc(json['reviewedAt'] as String)
          : null,
      reviewNotes: json['reviewNotes'] as String?,
      submittedAt: json['submittedAt'] != null
          ? DateFormatter.parseUtc(json['submittedAt'] as String)
          : DateTime.now(),
      evidencePhotoUrl: json['evidencePhotoUrl'] as String?,
      originalRejectionReason: json['originalRejectionReason'] as String?,
      entityTitle: json['entityTitle'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'appealId': appealId,
      'appealType': appealType,
      'appealTypeName': appealTypeName,
      'entityType': entityType,
      'entityId': entityId,
      'userId': userId,
      'workerName': workerName,
      'workerExplanation': workerExplanation,
      'workerLatitude': workerLatitude,
      'workerLongitude': workerLongitude,
      'expectedLatitude': expectedLatitude,
      'expectedLongitude': expectedLongitude,
      'distanceMeters': distanceMeters,
      'status': status,
      'statusName': statusName,
      'reviewedByUserId': reviewedByUserId,
      'reviewedByName': reviewedByName,
      'reviewedAt': reviewedAt?.toIso8601String(),
      'reviewNotes': reviewNotes,
      'submittedAt': submittedAt.toIso8601String(),
      'evidencePhotoUrl': evidencePhotoUrl,
      'originalRejectionReason': originalRejectionReason,
      'entityTitle': entityTitle,
    };
  }

  bool get isPending => status == 'Pending';
  bool get isApproved => status == 'Approved';
  bool get isRejected => status == 'Rejected';

  String get statusColor {
    switch (status) {
      case 'Pending':
        return 'orange';
      case 'Approved':
        return 'green';
      case 'Rejected':
        return 'red';
      default:
        return 'grey';
    }
  }

  Color get statusColorValue {
    switch (status) {
      case 'Approved':
        return Colors.green;
      case 'Rejected':
        return Colors.red;
      case 'Pending':
        return Colors.orange;
      default:
        return Colors.grey;
    }
  }
}
