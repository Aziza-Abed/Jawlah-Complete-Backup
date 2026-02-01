import 'dart:io';
import 'package:dio/dio.dart';

import '../../core/errors/app_exception.dart';
import '../models/appeal_model.dart';
import 'api_service.dart';

class AppealsService {
  final ApiService _apiService = ApiService();

  // Appeals endpoints (consistent with other services)
  static const String _appeals = 'appeals';
  static const String _myAppeals = 'appeals/my-appeals';

  /// Submit an appeal against an auto-rejected task
  Future<int> submitAppeal({
    required int taskId,
    required String explanation,
    File? evidencePhoto,
  }) async {
    try {
      final formData = FormData.fromMap({
        'AppealType': 1, // TaskRejection = 1
        'EntityId': taskId,
        'WorkerExplanation': explanation,
      });

      if (evidencePhoto != null) {
        formData.files.add(MapEntry(
          'EvidencePhoto',
          await MultipartFile.fromFile(
            evidencePhoto.path,
            filename: 'evidence_${DateTime.now().millisecondsSinceEpoch}.jpg',
          ),
        ));
      }

      final response = await _apiService.post(
        _appeals,
        data: formData,
      );

      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ?? 'فشل في إرسال الطعن',
        );
      }

      return responseData['data']['appealId'] as int;
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل في إرسال الطعن');
    }
  }

  /// Get all appeals for the current user
  Future<List<AppealModel>> getMyAppeals() async {
    try {
      final response = await _apiService.get(_myAppeals);

      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ?? 'فشل في تحميل الطعون',
        );
      }

      final List<dynamic> data = responseData['data'] as List<dynamic>;
      return data.map((json) => AppealModel.fromJson(json)).toList();
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل في تحميل الطعون');
    }
  }

  /// Get appeal by ID
  Future<AppealModel> getAppealById(int appealId) async {
    try {
      final response = await _apiService.get('$_appeals/$appealId');

      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ?? 'فشل في تحميل الطعن',
        );
      }

      return AppealModel.fromJson(responseData['data']);
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل في تحميل الطعن');
    }
  }
}
