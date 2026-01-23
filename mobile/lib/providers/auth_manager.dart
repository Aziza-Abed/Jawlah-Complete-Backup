import 'dart:convert';
import 'dart:math';
import 'package:crypto/crypto.dart';
import 'package:flutter/foundation.dart';
import '../core/utils/hive_init.dart';
import '../core/utils/background_service_utils.dart';
import '../data/models/user_model.dart';
import '../data/services/auth_service.dart';
import '../data/services/attendance_service.dart';
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
  bool _isCheckedIn = false;
  int? _activeAttendanceId;

  // Last login check-in result details
  String _lastCheckInStatus = 'NotAttempted';
  String? _lastCheckInFailureReason;
  bool _lastLoginRequiresApproval = false;
  bool _lastLoginIsLate = false;
  int _lastLoginLateMinutes = 0;
  String _lastLoginAttendanceType = 'OnTime';
  String? _lastLoginMessage;

  // OTP (Two-Factor Authentication) state
  bool _requiresOtp = false;
  String? _otpSessionToken;
  String? _otpMaskedPhone;

  UserModel? get user => currentUser;
  String? get token => jwtToken;
  bool get isAuthenticated => jwtToken != null && currentUser != null;
  String get userName => currentUser?.fullName ?? 'موظف';
  bool get isCheckedIn => _isCheckedIn;
  int? get activeAttendanceId => _activeAttendanceId;

  // Expose last login check-in details
  String get lastCheckInStatus => _lastCheckInStatus;
  String? get lastCheckInFailureReason => _lastCheckInFailureReason;
  bool get lastLoginRequiresApproval => _lastLoginRequiresApproval;
  bool get lastLoginIsLate => _lastLoginIsLate;
  int get lastLoginLateMinutes => _lastLoginLateMinutes;
  String get lastLoginAttendanceType => _lastLoginAttendanceType;
  String? get lastLoginMessage => _lastLoginMessage;
  bool get checkInFailed => _lastCheckInStatus == 'Failed';
  bool get needsManualCheckIn => !_isCheckedIn && _lastCheckInStatus != 'Success';

  // OTP (Two-Factor Authentication) getters
  bool get requiresOtp => _requiresOtp;
  String? get otpSessionToken => _otpSessionToken;
  String? get otpMaskedPhone => _otpMaskedPhone;

  // method to update check-in status (called by AttendanceManager)
  void updateCheckInStatus(bool isCheckedIn, {int? attendanceId}) {
    _isCheckedIn = isCheckedIn;
    _activeAttendanceId = attendanceId;
    notifyListeners();
  }

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
          // token maybe old so clear evrything
          jwtToken = null;
          currentUser = null;
          ApiService().updateToken(null);
          await _storageService.clearAll();
        }

        notifyListeners();

        // register FCM token if logged in
        if (jwtToken != null) {
          await _registerFcmToken();

          // check if user has active attendance to decide on background tracking
          // background tracking should only start after check-in, not just login
          try {
            final attendanceService = AttendanceService();
            final todayAttendance = await attendanceService.getTodayAttendance();
            if (todayAttendance != null && todayAttendance.isActive) {
              _isCheckedIn = true;
              _activeAttendanceId = todayAttendance.attendanceId;
              BackgroundServiceUtils.startService();
              BatteryService().startMonitoring();
            }
          } catch (e) {
            // ignore - will be handled when home screen loads attendance
            if (kDebugMode) debugPrint('Could not check attendance status: $e');
          }
        }
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Error loading session: $e');
    }
  }

  // SECURITY FIX: Use PBKDF2 with salt instead of plain SHA256
  // This prevents rainbow table attacks on stored offline passwords
  static const int _pbkdf2Iterations = 100000;
  static const int _saltLength = 32;
  static const int _keyLength = 32;

  // Generate a cryptographically secure random salt
  String _generateSalt() {
    final random = Random.secure();
    final salt = Uint8List(_saltLength);
    for (var i = 0; i < _saltLength; i++) {
      salt[i] = random.nextInt(256);
    }
    return base64Encode(salt);
  }

  // PBKDF2-HMAC-SHA256 key derivation
  String _pbkdf2(String password, String saltBase64) {
    final salt = base64Decode(saltBase64);
    final passwordBytes = utf8.encode(password);

    // PBKDF2 implementation using HMAC-SHA256
    var block = Uint8List(_keyLength);
    var hmacKey = Hmac(sha256, passwordBytes);

    for (var blockNum = 1; blockNum <= 1; blockNum++) {
      // For our key length, we only need 1 block
      var u = Uint8List(salt.length + 4);
      u.setRange(0, salt.length, salt);
      u[salt.length] = (blockNum >> 24) & 0xff;
      u[salt.length + 1] = (blockNum >> 16) & 0xff;
      u[salt.length + 2] = (blockNum >> 8) & 0xff;
      u[salt.length + 3] = blockNum & 0xff;

      var uPrev = Uint8List.fromList(hmacKey.convert(u).bytes);
      block = Uint8List.fromList(uPrev);

      for (var i = 1; i < _pbkdf2Iterations; i++) {
        uPrev = Uint8List.fromList(hmacKey.convert(uPrev).bytes);
        for (var j = 0; j < _keyLength; j++) {
          block[j] ^= uPrev[j];
        }
      }
    }

    return base64Encode(block);
  }

  // Hash password with new salt (for storing)
  String _hashPasswordWithSalt(String password) {
    final salt = _generateSalt();
    final hash = _pbkdf2(password, salt);
    // Store both salt and hash, separated by ':'
    return '$salt:$hash';
  }

  // Verify password against stored salted hash
  bool _verifyPasswordHash(String password, String storedHash) {
    try {
      final parts = storedHash.split(':');
      if (parts.length != 2) {
        // Legacy format (plain SHA256) - upgrade on next login
        return sha256.convert(utf8.encode(password)).toString() == storedHash;
      }
      final salt = parts[0];
      final expectedHash = parts[1];
      final actualHash = _pbkdf2(password, salt);
      return actualHash == expectedHash;
    } catch (e) {
      if (kDebugMode) debugPrint('Password verification error: $e');
      return false;
    }
  }

  // Login with Username + Password + GPS + DeviceID for auto check-in
  // If location is provided and valid, worker is automatically checked in
  // GPS failure fallback: If allowManualCheckIn=true and manualReason provided,
  // creates pending attendance that requires supervisor approval
  Future<bool> doLogin(
    String username, {
    required String password,
    double? latitude,
    double? longitude,
    double? accuracy,
    bool allowManualCheckIn = false,
    String? manualCheckInReason,
  }) async {
    // Reset check-in status before login attempt
    _lastCheckInStatus = 'NotAttempted';
    _lastCheckInFailureReason = null;
    _lastLoginRequiresApproval = false;
    _lastLoginIsLate = false;
    _lastLoginLateMinutes = 0;
    _lastLoginAttendanceType = 'OnTime';
    _lastLoginMessage = null;

    // Reset OTP state
    _requiresOtp = false;
    _otpSessionToken = null;
    _otpMaskedPhone = null;

    final result = await executeWithErrorHandling(() async {
      // try to login with Username + Password + GPS + DeviceID
      final loginResult = await _authService.loginWithGPS(
        username: username,
        password: password,
        latitude: latitude,
        longitude: longitude,
        accuracy: accuracy,
        allowManualCheckIn: allowManualCheckIn,
        manualCheckInReason: manualCheckInReason,
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
      _isCheckedIn = loginResult.isCheckedIn;
      _activeAttendanceId = loginResult.activeAttendanceId;

      // Store check-in result details
      _lastCheckInStatus = loginResult.checkInStatus;
      _lastCheckInFailureReason = loginResult.checkInFailureReason;
      _lastLoginRequiresApproval = loginResult.requiresApproval;
      _lastLoginIsLate = loginResult.isLate;
      _lastLoginLateMinutes = loginResult.lateMinutes;
      _lastLoginAttendanceType = loginResult.attendanceType;
      _lastLoginMessage = loginResult.message;

      // Clear old local data on fresh login (server is source of truth)
      // This prevents stale "pending sync" indicators
      try {
        await HiveInit.clearAllData();
        if (kDebugMode) debugPrint('Cleared old local data on fresh login');
      } catch (e) {
        if (kDebugMode) debugPrint('Failed to clear old data: $e');
      }

      // save user data
      await _storageService.saveToken(loginResult.token!);
      await _storageService.saveUserId(loginResult.user!.userId);

      await _storageService.saveHashedPin(_hashPasswordWithSalt(password));
      await _storageService.saveUserProfile(jsonEncode(loginResult.user!.toJson()));

      // update api token
      ApiService().updateToken(loginResult.token);

      notifyListeners();

      // start gps tracking only if already checked in
      // otherwise wait for user to check in
      if (_isCheckedIn) {
        BackgroundServiceUtils.startService();
        BatteryService().startMonitoring();
      }

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
      final savedHashedPassword = await _storageService.getHashedPin(); // Method name stays same for backward compat
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

    // auto-checkout if still checked in
    if (_isCheckedIn) {
      try {
        final attendanceService = AttendanceService();
        await attendanceService.checkOut();
        if (kDebugMode) debugPrint('Auto-checkout on logout successful');
      } catch (e) {
        // ignore checkout errors during logout
        if (kDebugMode) debugPrint('Auto-checkout on logout failed (ignored): $e');
      }
    }

    // unregister FCM token before logout
    try {
      final fcmService = FirebaseMessagingService();
      await fcmService.unregisterFcmToken();
      if (kDebugMode) debugPrint('FCM token unregistered on logout');
    } catch (e) {
      // ignore FCM errors during logout
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
    _isCheckedIn = false;
    _activeAttendanceId = null;
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
    double? latitude,
    double? longitude,
    double? accuracy,
  }) async {
    final result = await executeWithErrorHandling(() async {
      final verifyResult = await _authService.verifyOtp(
        sessionToken: sessionToken,
        otpCode: otpCode,
      );

      // OTP verified successfully - complete login
      currentUser = verifyResult.user;
      jwtToken = verifyResult.token;
      _isCheckedIn = verifyResult.isCheckedIn;
      _activeAttendanceId = verifyResult.activeAttendanceId;

      // Store check-in result details
      _lastCheckInStatus = verifyResult.checkInStatus;
      _lastCheckInFailureReason = verifyResult.checkInFailureReason;
      _lastLoginRequiresApproval = verifyResult.requiresApproval;
      _lastLoginIsLate = verifyResult.isLate;
      _lastLoginLateMinutes = verifyResult.lateMinutes;
      _lastLoginAttendanceType = verifyResult.attendanceType;
      _lastLoginMessage = verifyResult.message;

      // Reset OTP state
      _requiresOtp = false;
      _otpSessionToken = null;
      _otpMaskedPhone = null;

      // save user data
      await _storageService.saveToken(verifyResult.token!);
      await _storageService.saveUserId(verifyResult.user!.userId);
      await _storageService.saveUserProfile(jsonEncode(verifyResult.user!.toJson()));

      // update api token
      ApiService().updateToken(verifyResult.token);

      notifyListeners();

      // start gps tracking only if already checked in
      if (_isCheckedIn) {
        BackgroundServiceUtils.startService();
        BatteryService().startMonitoring();
      }

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
