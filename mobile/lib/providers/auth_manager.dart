import 'dart:convert';
import 'dart:math';
import 'package:crypto/crypto.dart';
import 'package:flutter/foundation.dart';
import '../core/utils/hive_init.dart';
import '../core/utils/background_service_utils.dart';
import '../data/models/user_model.dart';
import '../data/services/auth_service.dart';
import '../data/services/storage_service.dart';
import '../data/services/api_service.dart';
import '../data/services/firebase_messaging_service.dart';
import '../data/services/battery_service.dart';
import '../core/utils/secure_storage_helper.dart';

import 'base_controller.dart';

class AuthManager extends BaseController {
  final AuthService _authService;
  final StorageService _storageService;

  UserModel? currentUser;
  String? jwtToken;

  // OTP (Two-Factor Authentication) state
  bool _requiresOtp = false;
  String? _otpSessionToken;
  String? _otpMaskedPhone;

  UserModel? get user => currentUser;
  String? get token => jwtToken;
  bool get isAuthenticated => jwtToken != null && currentUser != null;
  String get userName => currentUser?.fullName ?? 'موظف';

  // OTP (Two-Factor Authentication) getters
  bool get requiresOtp => _requiresOtp;
  String? get otpSessionToken => _otpSessionToken;
  String? get otpMaskedPhone => _otpMaskedPhone;

  AuthManager(this._authService, this._storageService) {
    _loadSavedSession();
  }

  // load user session from storage when app starts
  Future<void> _loadSavedSession() async {
    try {
      // check if we have token and user id saved
      final token = await _storageService.getToken();
      final userId = await _storageService.getUserId();

      if (token != null && userId != null) {
        jwtToken = token;
        ApiService().updateToken(token);

        // get user data from server
        try {
          final user = await _authService.getProfile();
          currentUser = user;
        } catch (e) {
          // token maybe old so clear everything
          jwtToken = null;
          currentUser = null;
          ApiService().updateToken(null);
          await _storageService.clearAll();
        }

        notifyListeners();

        // register FCM token if logged in
        if (jwtToken != null) {
          await _registerFcmToken();

          // UC4: Start background service for GPS tracking and auto-geofencing
          // Background service handles automatic check-in/out via geofencing
          BackgroundServiceUtils.startService();
          BatteryService().startMonitoring();
        }
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Error loading session: $e');
    }
  }

  // Offline password hashing using SHA256 with salt
  static const int _saltLength = 16;

  String _generateSalt() {
    final random = Random.secure();
    final salt = List<int>.generate(_saltLength, (_) => random.nextInt(256));
    return base64Encode(salt);
  }

  String _hashPasswordWithSalt(String password) {
    final salt = _generateSalt();
    final hash = sha256.convert(utf8.encode(salt + password)).toString();
    return '$salt:$hash';
  }

  bool _verifyPasswordHash(String password, String storedHash) {
    try {
      final parts = storedHash.split(':');
      if (parts.length != 2) {
        return sha256.convert(utf8.encode(password)).toString() == storedHash;
      }
      final salt = parts[0];
      final expectedHash = parts[1];
      final actualHash = sha256.convert(utf8.encode(salt + password)).toString();
      return actualHash == expectedHash;
    } catch (e) {
      if (kDebugMode) debugPrint('Password verification error: $e');
      return false;
    }
  }

  // UC2: Login is pure authentication. No attendance check-in.
  // UC4: Attendance is handled automatically via geofencing in background service.
  Future<bool> doLogin(
    String username, {
    required String password,
  }) async {
    // Reset OTP state
    _requiresOtp = false;
    _otpSessionToken = null;
    _otpMaskedPhone = null;

    final result = await executeWithErrorHandling(() async {
      final loginResult = await _authService.loginWithGPS(
        username: username,
        password: password,
      );

      // Check if OTP is required (Two-Factor Authentication)
      if (loginResult.requiresOtp) {
        _requiresOtp = true;
        _otpSessionToken = loginResult.sessionToken;
        _otpMaskedPhone = loginResult.maskedPhone;
        notifyListeners();
        return loginResult;
      }

      currentUser = loginResult.user;
      jwtToken = loginResult.token;

      // Clear old local data on fresh login (server is source of truth)
      try {
        await HiveInit.clearAllData();
        if (kDebugMode) debugPrint('Cleared old local data on fresh login');
      } catch (e) {
        if (kDebugMode) debugPrint('Failed to clear old data: $e');
      }

      // save user data
      await _storageService.saveToken(loginResult.token!);
      if (loginResult.refreshToken != null) {
        await _storageService.saveRefreshToken(loginResult.refreshToken!);
      }
      await _storageService.saveUserId(loginResult.user!.userId);

      await _storageService.saveHashedPin(_hashPasswordWithSalt(password));
      await _storageService.saveUserProfile(jsonEncode(loginResult.user!.toJson()));

      // update api token
      ApiService().updateToken(loginResult.token);
      ApiService().updateRefreshToken(loginResult.refreshToken);

      notifyListeners();

      // UC4: Start background service for GPS tracking and auto-geofencing
      BackgroundServiceUtils.startService();
      BatteryService().startMonitoring();

      try {
        await _registerFcmToken();
      } catch (e) {
        // ignore if fcm fails
      }

      return loginResult;
    });

    if (result == null) {
      // login failed maybe we are offline so try offline login
      return await _attemptOfflineLogin(password);
    }

    return true;
  }

  Future<bool> _attemptOfflineLogin(String password) async {
    try {
      final savedHashedPassword = await _storageService.getHashedPin();
      final savedUserProfile = await _storageService.getUserProfile();

      if (savedHashedPassword == null || savedUserProfile == null) {
        setError('يجب تسجيل الدخول عبر الإنترنت لأول مرة');
        return false;
      }

      if (!_verifyPasswordHash(password, savedHashedPassword)) {
        setError('كلمة المرور غير صحيحة');
        return false;
      }

      final userJson = jsonDecode(savedUserProfile) as Map<String, dynamic>;
      currentUser = UserModel.fromJson(userJson);

      jwtToken = await _storageService.getToken();

      if (jwtToken != null) {
        ApiService().updateToken(jwtToken);
      }

      notifyListeners();

      setError('وضع عدم الاتصال - قم بالمزامنة عند الاتصال بالإنترنت');

      return true;
    } catch (e) {
      setError('فشل تسجيل الدخول بدون اتصال');
      return false;
    }
  }

  Future<void> _registerFcmToken() async {
    final fcmService = FirebaseMessagingService();
    final savedToken = await SecureStorageHelper.getFcmToken();
    final tokenToSend = savedToken ?? fcmService.fcmToken;

    if (tokenToSend != null && tokenToSend.isNotEmpty) {
      await fcmService.registerFcmTokenWithBackend(tokenToSend);
    }
  }

  Future<bool> getRememberMe() async {
    return await _storageService.getRememberMe();
  }

  Future<void> setRememberMe(bool value) async {
    await _storageService.saveRememberMe(value);
  }

  Future<String?> getSavedEmployeeId() async {
    return await _storageService.getEmployeeId();
  }

  Future<void> saveEmployeeId(String employeeId) async {
    await _storageService.saveEmployeeId(employeeId);
  }

  Future<void> clearSavedEmployeeId() async {
    await _storageService.removeEmployeeId();
  }

  Future<void> doLogout() async {
    // stop battery monitoring
    BatteryService().stopMonitoring();

    // unregister FCM token before logout
    try {
      final fcmService = FirebaseMessagingService();
      await fcmService.unregisterFcmToken();
      if (kDebugMode) debugPrint('FCM token unregistered on logout');
    } catch (e) {
      if (kDebugMode) debugPrint('FCM unregister failed (ignored): $e');
    }

    try {
      await _authService.logout();
    } catch (e) {
      // ignore
    }

    await BackgroundServiceUtils.stopService();

    await _storageService.clearAll();

    try {
      await HiveInit.clearAllData();
    } catch (e) {
      if (kDebugMode) debugPrint('$e');
    }

    currentUser = null;
    jwtToken = null;
    _requiresOtp = false;
    _otpSessionToken = null;
    _otpMaskedPhone = null;
    clearError();

    ApiService().updateToken(null);

    notifyListeners();
  }

  // Verify OTP code for Two-Factor Authentication
  Future<bool> verifyOtp({
    required String sessionToken,
    required String otpCode,
  }) async {
    final result = await executeWithErrorHandling(() async {
      final verifyResult = await _authService.verifyOtp(
        sessionToken: sessionToken,
        otpCode: otpCode,
      );

      // OTP verified successfully - complete login
      currentUser = verifyResult.user;
      jwtToken = verifyResult.token;

      // Reset OTP state
      _requiresOtp = false;
      _otpSessionToken = null;
      _otpMaskedPhone = null;

      // save user data
      await _storageService.saveToken(verifyResult.token!);
      if (verifyResult.refreshToken != null) {
        await _storageService.saveRefreshToken(verifyResult.refreshToken!);
      }
      await _storageService.saveUserId(verifyResult.user!.userId);
      await _storageService.saveUserProfile(jsonEncode(verifyResult.user!.toJson()));

      // update api token
      ApiService().updateToken(verifyResult.token);
      ApiService().updateRefreshToken(verifyResult.refreshToken);

      notifyListeners();

      // UC4: Start background service for GPS tracking and auto-geofencing
      BackgroundServiceUtils.startService();
      BatteryService().startMonitoring();

      try {
        await _registerFcmToken();
      } catch (e) {
        // ignore if fcm fails
      }

      return verifyResult;
    });

    return result != null;
  }

  // Resend OTP code
  Future<OtpResendResult?> resendOtp({required String sessionToken}) async {
    return await executeWithErrorHandling(() async {
      return await _authService.resendOtp(sessionToken: sessionToken);
    });
  }
}
