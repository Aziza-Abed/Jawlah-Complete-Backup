import 'package:flutter/foundation.dart';

import '../../core/config/api_config.dart';
import '../../core/errors/app_exception.dart';

import '../models/user_model.dart';
import 'api_service.dart';
import 'location_service.dart';

class AuthService {
  final ApiService _apiService = ApiService();

  // login by employeeId with GPS validation
  Future<AuthResult> loginWithGPS({
    required String employeeId,
  }) async {
    try {
      // 1. get the current position of the worker
      final position = await LocationService.getCurrentLocation();
      if (position == null) {
        throw ValidationException('فشل الحصول على موقع GPS.');
      }

      // 2. send the data to the server
      final response = await _apiService.post(
        ApiConfig.loginWithGPS,
        data: {
          'pin': employeeId, // backend expects 'pin', not 'employeeId'
          'latitude': position.latitude,
          'longitude': position.longitude,
        },
      );

      // 3. check if the response is ok
      if (response.statusCode != 200) {
        throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
          statusCode: response.statusCode,
        );
      }

      final responseData = response.data;

      // 4. check if the login was successful
      if (responseData['success'] != true) {
        throw ValidationException(
          responseData['message'] ?? 'الرقم السري غير صحيح.',
        );
      }

      // 5. extract the data and return it
      final data = responseData['data'];

      if (data == null || data['user'] == null || data['token'] == null) {
        throw ServerException(
            'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
      }

      return AuthResult(
        user: UserModel.fromJson(data['user'] as Map<String, dynamic>),
        token: data['token'] as String,
        refreshToken: data['refreshToken'] as String?,
      );
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
    }
  }

  // get current user profile
  Future<UserModel> getProfile() async {
    try {
      final response = await _apiService.get(ApiConfig.profile);

      if (response.statusCode == 200) {
        // get data from API response
        final responseData = response.data;
        if (responseData['success'] != true) {
          throw ServerException(
            responseData['message'] ??
                'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
          );
        }
        return UserModel.fromJson(responseData['data']);
      } else {
        throw ServerException(
            'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
      }
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
    }
  }

  // logout current user
  Future<void> logout() async {
    try {
      await _apiService.post(ApiConfig.logout);
    } catch (e) {
      // ignore logout errors as user is already logged out locally
      if (kDebugMode) debugPrint('Logout error (ignored): $e');
    }
  }

  // validate current location is within allowed zone
  Future<bool> isInWorkZone() async {
    try {
      final position = await LocationService.getCurrentLocation();
      if (position == null) return false;

      final response = await _apiService.post(
        ApiConfig.validateZone,
        data: {
          'latitude': position.latitude,
          'longitude': position.longitude,
        },
      );

      if (response.statusCode == 200) {
        // get data from API response
        final responseData = response.data;
        if (responseData['success'] != true) {
          return false;
        }
        return responseData['data']['isValid'] as bool;
      }

      return false;
    } catch (e) {
      return false;
    }
  }
}

class AuthResult {
  final UserModel user;
  final String token;
  final String? refreshToken;

  AuthResult({
    required this.user,
    required this.token,
    this.refreshToken,
  });
}
