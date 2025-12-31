import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:path/path.dart' as path;
import '../../core/errors/app_exception.dart';

import '../models/issue_model.dart';
import 'api_service.dart';
import 'location_service.dart';
import '../../core/config/api_config.dart';

class IssuesService {
  final ApiService _apiService = ApiService();

  // report a new issue with description, type and 3 photos (1 mandatory, 2 optional)
  Future<IssueModel> reportIssue({
    required String description,
    required String type,
    required File photo1,
    File? photo2,
    File? photo3,
    String? severity,
    double? latitude,
    double? longitude,
    String? locationDescription,
  }) async {
    try {
      double lat;
      double lng;

      // decide which location to use (given or current)
      if (latitude != null && longitude != null) {
        lat = latitude;
        lng = longitude;
      } else {
        final position = await LocationService.getCurrentLocation();
        if (position == null) {
          throw ValidationException('فشل الحصول على موقع GPS.');
        }
        lat = position.latitude;
        lng = position.longitude;
      }

      // prepare the data in a form (FormData)
      final formData = FormData();

      // log the issue type for debugging
      if (kDebugMode) {
        debugPrint('Reporting issue with type: "$type"');
      }

      formData.fields.addAll([
        MapEntry('description', description),
        MapEntry('type', type.trim()), // Remove extra spaces
        MapEntry('latitude', lat.toString()),
        MapEntry('longitude', lng.toString()),
      ]);

      if (severity != null) {
        formData.fields.add(MapEntry('severity', severity));
      }

      if (locationDescription != null) {
        formData.fields
            .add(MapEntry('locationDescription', locationDescription));
      }

      // add photo1 (mandatory)
      if (!await photo1.exists()) {
        throw ValidationException('البيانات المدخلة غير صحيحة.');
      }
      final fileSize1 = await photo1.length();
      if (fileSize1 > ApiConfig.maxImageSize) {
        throw ValidationException('البيانات المدخلة غير صحيحة.');
      }
      formData.files.add(
        MapEntry(
          'photo1',
          await MultipartFile.fromFile(
            photo1.path,
            filename: path.basename(photo1.path),
          ),
        ),
      );

      // add photo2 (optional)
      if (photo2 != null) {
        if (await photo2.exists()) {
          final fileSize2 = await photo2.length();
          if (fileSize2 <= ApiConfig.maxImageSize) {
            formData.files.add(
              MapEntry(
                'photo2',
                await MultipartFile.fromFile(
                  photo2.path,
                  filename: path.basename(photo2.path),
                ),
              ),
            );
          }
        }
      }

      // add photo3 (optional)
      if (photo3 != null) {
        if (await photo3.exists()) {
          final fileSize3 = await photo3.length();
          if (fileSize3 <= ApiConfig.maxImageSize) {
            formData.files.add(
              MapEntry(
                'photo3',
                await MultipartFile.fromFile(
                  photo3.path,
                  filename: path.basename(photo3.path),
                ),
              ),
            );
          }
        }
      }

      final response = await _apiService.post(
        ApiConfig.reportIssue,
        data: formData,
      );

      // get data from API response
      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ??
              'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
        );
      }
      return IssueModel.fromJson(responseData['data']);
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
    }
  }

  // get issues reported by current user (optional status filter)
  Future<List<IssueModel>> getMyIssues({String? status}) async {
    try {
      final queryParams = <String, dynamic>{};
      if (status != null) queryParams['status'] = status;

      final response = await _apiService.get(
        ApiConfig.myIssues,
        queryParameters: queryParams.isNotEmpty ? queryParams : null,
      );

      // get data from API response
      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ??
              'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
        );
      }
      final List<dynamic> data = responseData['data'] as List<dynamic>;
      return data.map((json) => IssueModel.fromJson(json)).toList();
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
    }
  }

  // get a single issue by id
  Future<IssueModel> getIssueById(int issueId) async {
    try {
      final response = await _apiService.get('${ApiConfig.myIssues}/$issueId');

      // get data from API response
      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ??
              'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
        );
      }
      return IssueModel.fromJson(responseData['data']);
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
    }
  }
}
