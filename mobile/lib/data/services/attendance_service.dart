import 'package:dio/dio.dart';

import '../../core/config/api_config.dart';
import '../../core/errors/app_exception.dart';

import '../models/attendance_model.dart';
import 'api_service.dart';
import 'location_service.dart';

class AttendanceService {
  final ApiService _apiService = ApiService();

  // create today's attendance check in with current GPS
  Future<AttendanceModel> checkIn() async {
    try {
      // get the current location coordinates
      final position = await LocationService.getCurrentLocation();
      if (position == null) {
        throw ValidationException('فشل الحصول على موقع GPS.');
      }

      // send the check in request to the server
      final response = await _apiService.post(
        ApiConfig.checkIn,
        data: {
          'latitude': position.latitude,
          'longitude': position.longitude,
        },
      );

      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ?? 'فشل تسجيل الحضور',
        );
      }
      // return the attendance data
      return AttendanceModel.fromJson(responseData['data']);
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل تسجيل الحضور');
    }
  }

  // close today's attendance with current GPS
  Future<AttendanceModel> checkOut() async {
    try {
      final position = await LocationService.getCurrentLocation();
      if (position == null) {
        throw ValidationException('فشل الحصول على موقع GPS.');
      }

      final response = await _apiService.post(
        ApiConfig.checkOut,
        data: {
          'latitude': position.latitude,
          'longitude': position.longitude,
        },
      );

      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ?? 'فشل تسجيل الانصراف',
        );
      }
      return AttendanceModel.fromJson(responseData['data']);
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل تسجيل الانصراف');
    }
  }

  // get today's attendance record if exists
  Future<AttendanceModel?> getTodayAttendance() async {
    try {
      final response = await _apiService.get(ApiConfig.todayAttendance);

      final responseData = response.data;
      if (responseData == null || responseData['data'] == null) {
        return null;
      }

      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ?? 'فشل في تحميل سجل الحضور',
        );
      }

      return AttendanceModel.fromJson(responseData['data']);
    } catch (e) {
      if (e is NotFoundException) return null;
      if (e is AppException) rethrow;

      // handle 404 as no attendance today
      if (e is DioException && e.response?.statusCode == 404) {
        return null;
      }

      throw ServerException('فشل في تحميل سجل الحضور');
    }
  }

  // get attendance history for current user optional range and pagination
  Future<List<AttendanceModel>> getMyAttendance({
    DateTime? fromDate,
    DateTime? toDate,
    int? page,
    int? pageSize,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (fromDate != null) {
        queryParams['fromDate'] = fromDate.toIso8601String();
      }
      if (toDate != null) {
        queryParams['toDate'] = toDate.toIso8601String();
      }
      if (page != null) {
        queryParams['page'] = page;
      }
      if (pageSize != null) {
        queryParams['pageSize'] = pageSize;
      }

      final response = await _apiService.get(
        ApiConfig.attendanceHistory,
        queryParameters: queryParams.isNotEmpty ? queryParams : null,
      );

      final responseData = response.data;
      if (responseData['success'] != true) {
        throw ServerException(
          responseData['message'] ?? 'فشل في تحميل سجل الحضور',
        );
      }

      final List<dynamic> data = responseData['data'] as List<dynamic>;
      return data.map((json) => AttendanceModel.fromJson(json)).toList();
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException('فشل في تحميل سجل الحضور');
    }
  }
}
