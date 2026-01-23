import 'package:flutter/foundation.dart';

import '../../core/config/api_config.dart';
import '../../core/errors/app_exception.dart';

import '../models/user_model.dart';
import 'api_service.dart';
import 'device_service.dart';

class AuthService {
  final ApiService _apiService = ApiService();

  // Login with Username + Password + GPS + DeviceID for auto check-in
  // If location is provided and valid, worker is automatically checked in
  // GPS failure fallback: If allowManualCheckIn=true and manualReason provided,
  // creates pending attendance that requires supervisor approval
  Future<AuthResult> loginWithGPS({
    required String username,
    required String password,
    double? latitude,
    double? longitude,
    double? accuracy,
    bool allowManualCheckIn = false,
    String? manualCheckInReason,
  }) async {
    try {
      // get unique device ID for device binding security (2FA)
      final deviceId = await DeviceService.getDeviceId();

      // build request data
      final requestData = <String, dynamic>{
        'username': username,
        'password': password,
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
        ApiConfig.loginWithGPS,
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
          responseData['message'] ?? 'اسم المستخدم أو كلمة المرور غير صحيحة.',
        );
      }

      // extract the data and return it
      final data = responseData['data'];

      // Check if OTP is required (Two-Factor Authentication)
      if (data != null && data['requiresOtp'] == true) {
        return AuthResult(
          requiresOtp: true,
          sessionToken: data['sessionToken'] as String?,
          maskedPhone: data['maskedPhone'] as String?,
          message: responseData['message'] as String?,
        );
      }

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

  // Verify OTP code for Two-Factor Authentication
  Future<AuthResult> verifyOtp({
    required String sessionToken,
    required String otpCode,
    String? deviceId,
  }) async {
    try {
      final requestData = <String, dynamic>{
        'sessionToken': sessionToken,
        'otpCode': otpCode,
      };

      if (deviceId != null) {
        requestData['deviceId'] = deviceId;
      }

      final response = await _apiService.post(
        ApiConfig.verifyOtp,
        data: requestData,
      );

      if (response.statusCode != 200) {
        throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
          statusCode: response.statusCode,
        );
      }

      final responseData = response.data;

      if (responseData['success'] != true) {
        // Extract remaining attempts if available
        final data = responseData['data'];
        final remainingAttempts = data?['remainingAttempts'] as int?;
        String errorMsg = responseData['message'] ?? 'رمز التحقق غير صحيح';
        if (remainingAttempts != null && remainingAttempts > 0) {
          errorMsg = '$errorMsg\nالمحاولات المتبقية: $remainingAttempts';
        }
        throw ValidationException(errorMsg);
      }

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

  // Resend OTP code
  Future<OtpResendResult> resendOtp({
    required String sessionToken,
    String? deviceId,
  }) async {
    try {
      final requestData = <String, dynamic>{
        'sessionToken': sessionToken,
      };

      if (deviceId != null) {
        requestData['deviceId'] = deviceId;
      }

      final response = await _apiService.post(
        ApiConfig.resendOtp,
        data: requestData,
      );

      if (response.statusCode != 200) {
        throw ServerException(
          'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.',
          statusCode: response.statusCode,
        );
      }

      final responseData = response.data;

      if (responseData['success'] != true) {
        throw ValidationException(
          responseData['message'] ?? 'فشل إعادة إرسال رمز التحقق',
        );
      }

      final data = responseData['data'];

      return OtpResendResult(
        success: true,
        maskedPhone: data?['maskedPhone'] as String? ?? '',
        message: responseData['message'] as String? ?? 'تم إرسال رمز التحقق',
        cooldownSeconds: data?['resendCooldownSeconds'] as int? ?? 60,
      );
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

// Result class for OTP resend
class OtpResendResult {
  final bool success;
  final String maskedPhone;
  final String message;
  final int cooldownSeconds;

  OtpResendResult({
    required this.success,
    required this.maskedPhone,
    required this.message,
    required this.cooldownSeconds,
  });
}

class AuthResult {
  final UserModel? user;
  final String? token;
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

  // OTP (Two-Factor Authentication) fields
  final bool requiresOtp;
  final String? sessionToken;
  final String? maskedPhone;

  AuthResult({
    this.user,
    this.token,
    this.isCheckedIn = false,
    this.activeAttendanceId,
    this.checkInStatus = 'NotAttempted',
    this.checkInFailureReason,
    this.requiresApproval = false,
    this.isLate = false,
    this.lateMinutes = 0,
    this.attendanceType = 'OnTime',
    this.message,
    this.requiresOtp = false,
    this.sessionToken,
    this.maskedPhone,
  });

  // Helper to check if check-in was attempted but failed
  bool get checkInFailed => checkInStatus == 'Failed';

  // Helper to check if we need to show GPS failure dialog
  bool get needsManualCheckIn => !isCheckedIn && checkInStatus != 'Success';
}
