import 'package:flutter/foundation.dart';

import '../../core/config/api_config.dart';
import '../../core/errors/app_exception.dart';

import '../models/user_model.dart';
import 'api_service.dart';
import 'device_service.dart';

class AuthService {
  final ApiService _apiService = ApiService();

  // Login with PIN and optional location for auto check-in
  // If location is provided and valid, worker is automatically checked in
  // GPS failure fallback: If allowManualCheckIn=true and manualReason provided,
  // creates pending attendance that requires supervisor approval
  Future<AuthResult> loginWithPin({
    required String pin,
    double? latitude,
    double? longitude,
    double? accuracy,
    bool allowManualCheckIn = false,
    String? manualCheckInReason,
  }) async {
    try {
      // get unique device ID for device binding security
      final deviceId = await DeviceService.getDeviceId();

      // build request data
      final requestData = <String, dynamic>{
        'pin': pin,
        'deviceId': deviceId,
      };

      // add location if provided
      if (latitude != null && longitude != null) {
        requestData['latitude'] = latitude;
        requestData['longitude'] = longitude;
        if (accuracy != null) {
          requestData['accuracy'] = accuracy;
        }
      }

      // add manual check-in request if GPS failed
      if (allowManualCheckIn && manualCheckInReason != null) {
        requestData['allowManualCheckIn'] = true;
        requestData['manualCheckInReason'] = manualCheckInReason;
      }

      // send the data to the server
      final response = await _apiService.post(
        ApiConfig.loginWithPin,
        data: requestData,
      );

      // check if the response is ok
      if (response.statusCode != 200) {
        throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
          statusCode: response.statusCode,
        );
      }

      final responseData = response.data;

      // check if the login was successful
      if (responseData['success'] != true) {
        throw ValidationException(
          responseData['message'] ?? 'الرقم السري غير صحيح.',
        );
      }

      // extract the data and return it
      final data = responseData['data'];

      if (data == null || data['user'] == null || data['token'] == null) {
        throw ServerException(
            'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
      }

      return AuthResult(
        user: UserModel.fromJson(data['user'] as Map<String, dynamic>),
        token: data['token'] as String,
        isCheckedIn: data['isCheckedIn'] as bool? ?? false,
        activeAttendanceId: data['activeAttendanceId'] as int?,
        checkInStatus: data['checkInStatus'] as String? ?? 'NotAttempted',
        checkInFailureReason: data['checkInFailureReason'] as String?,
        requiresApproval: data['requiresApproval'] as bool? ?? false,
        isLate: data['isLate'] as bool? ?? false,
        lateMinutes: data['lateMinutes'] as int? ?? 0,
        attendanceType: data['attendanceType'] as String? ?? 'OnTime',
        message: data['message'] as String?,
      );
    } catch (e) {
      if (e is AppException) rethrow;
      throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
    }
  }

  // get current user profile from server
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
      // ignore logout errors as user is alredy logged out locally
      if (kDebugMode) debugPrint('Logout error (ignored): $e');
    }
  }
}

class AuthResult {
  final UserModel user;
  final String token;
  final bool isCheckedIn;
  final int? activeAttendanceId;

  // Check-in status details
  final String checkInStatus; // NotAttempted, Success, PendingApproval, Failed
  final String? checkInFailureReason;
  final bool requiresApproval; // Manual check-in needs supervisor approval

  // Lateness tracking
  final bool isLate;
  final int lateMinutes;
  final String attendanceType; // OnTime, Late, Manual

  // Server message
  final String? message;

  AuthResult({
    required this.user,
    required this.token,
    this.isCheckedIn = false,
    this.activeAttendanceId,
    this.checkInStatus = 'NotAttempted',
    this.checkInFailureReason,
    this.requiresApproval = false,
    this.isLate = false,
    this.lateMinutes = 0,
    this.attendanceType = 'OnTime',
    this.message,
  });

  // Helper to check if check-in was attempted but failed
  bool get checkInFailed => checkInStatus == 'Failed';

  // Helper to check if we need to show GPS failure dialog
  bool get needsManualCheckIn => !isCheckedIn && checkInStatus != 'Success';
}
