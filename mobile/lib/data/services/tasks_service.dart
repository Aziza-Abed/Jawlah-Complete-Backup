import 'dart:io';

import 'package:dio/dio.dart';
import 'package:path/path.dart' as path;

import '../../core/config/api_config.dart';
import '../../core/errors/app_exception.dart';

import '../models/task_model.dart';
import 'api_service.dart';

class TasksService {
  final ApiService _apiService = ApiService();

  // get tasks assigned to current user with optional filters and pagination
  Future<List<TaskModel>> getMyTasks({
    String? status,
    String? priority,
    int? page,
    int? pageSize,
  }) async {
    // prepare the filters
    final queryParams = <String, dynamic>{};
    if (status != null) queryParams['status'] = status;
    if (priority != null) queryParams['priority'] = priority;
    if (page != null) queryParams['page'] = page;
    if (pageSize != null) queryParams['pageSize'] = pageSize;

    // call the api
    final response = await _apiService.get(
      ApiConfig.myTasks,
      queryParameters: queryParams.isNotEmpty ? queryParams : null,
    );

    // check if the response is ok and return the tasks
    final responseData = response.data;
    if (responseData['success'] != true) {
      throw ServerException(
        responseData['message'] ?? 'فشل تحميل المهام',
      );
    }
    final List<dynamic> data = responseData['data'] as List<dynamic>;
    return data.map((json) => TaskModel.fromJson(json)).toList();
  }

  // get single task details by id
  Future<TaskModel> getTaskById(int taskId) async {
    final response = await _apiService.get('${ApiConfig.taskDetails}/$taskId');

    // get data from API response
    final responseData = response.data;
    if (responseData['success'] != true) {
      throw ServerException(
        responseData['message'] ?? 'المهمة غير موجودة.',
      );
    }
    return TaskModel.fromJson(responseData['data']);
  }

  // update task status and optional notes
  Future<TaskModel> updateTaskStatus(
    int taskId,
    String newStatus, {
    String? notes,
  }) async {
    // send status in camelCase format backend now configured for camelCase JSON
    final response = await _apiService.put(
      '${ApiConfig.updateTaskStatus}/$taskId/status',
      data: {
        'status': newStatus,
        if (notes != null) 'completionNotes': notes,
      },
    );

    // get data from API response
    final responseData = response.data;
    if (responseData['success'] != true) {
      throw ServerException(
        responseData['message'] ??
            'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
      );
    }
    return TaskModel.fromJson(responseData['data']);
  }

  // complete task with optional photo and notes
  Future<TaskModel> completeTask(
    int taskId, {
    required String notes,
    File? proofPhoto,
    double? latitude,
    double? longitude,
  }) async {
    final formData = FormData();

    formData.fields.add(MapEntry('completionNotes', notes));

    // include GPS coordinates if we have them
    if (latitude != null) {
      formData.fields.add(MapEntry('latitude', latitude.toString()));
    }
    if (longitude != null) {
      formData.fields.add(MapEntry('longitude', longitude.toString()));
    }

    if (proofPhoto != null) {
      if (!await proofPhoto.exists()) {
        throw ValidationException('البيانات المدخلة غير صحيحة.');
      }

      final fileSize = await proofPhoto.length();
      if (fileSize > ApiConfig.maxImageSize) {
        throw ValidationException('البيانات المدخلة غير صحيحة.');
      }

      formData.files.add(
        MapEntry(
          'photo',
          await MultipartFile.fromFile(
            proofPhoto.path,
            filename: path.basename(proofPhoto.path),
          ),
        ),
      );
    }

    final response = await _apiService.post(
      '${ApiConfig.completeTask}/$taskId/complete',
      data: formData,
    );

    // get data from API response
    final responseData = response.data;
    if (responseData['success'] != true) {
      throw ServerException(
        responseData['message'] ?? 'فشل إكمال المهمة',
      );
    }
    return TaskModel.fromJson(responseData['data']);
  }

  // shortcut to set task to InProgress
  Future<TaskModel> startTask(int taskId) async {
    return await updateTaskStatus(
      taskId,
      'InProgress',
      notes: 'تم بدء المهمة',
    );
  }

  // update task progress (for multi-day tasks)
  Future<TaskModel?> updateTaskProgress(
    int taskId,
    int progressPercentage,
  ) async {
    try {
      final response = await _apiService.put(
        '${ApiConfig.updateTaskProgress}/$taskId/progress',
        data: {
          'progressPercentage': progressPercentage,
        },
      );

      if (response.statusCode == 200 || response.statusCode == 201) {
        final responseData = response.data;
        if (responseData['success'] == true && responseData['data'] != null) {
          return TaskModel.fromJson(responseData['data']);
        }
      }

      return null;
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل تحديث التقدم');
    }
  }
}
