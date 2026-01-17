import 'dart:convert';
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

  String _hashPin(String pin) {
    return sha256.convert(utf8.encode(pin)).toString();
  }

  // Login with PIN and optional location for auto check-in
  // If location is provided and valid, worker is automatically checked in
  // GPS failure fallback: If allowManualCheckIn=true and manualReason provided,
  // creates pending attendance that requires supervisor approval
  Future<bool> doLogin(
    String pin, {
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

    final result = await executeWithErrorHandling(() async {
      // try to login with PIN and optional location
      final loginResult = await _authService.loginWithPin(
        pin: pin,
        latitude: latitude,
        longitude: longitude,
        accuracy: accuracy,
        allowManualCheckIn: allowManualCheckIn,
        manualCheckInReason: manualCheckInReason,
      );

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

      // save user data
      await _storageService.saveToken(loginResult.token);
      await _storageService.saveUserId(loginResult.user.userId);
      if (loginResult.refreshToken != null &&
          loginResult.refreshToken!.isNotEmpty) {
        await _storageService.saveRefreshToken(loginResult.refreshToken!);
      }

      await _storageService.saveHashedPin(_hashPin(pin));
      await _storageService
          .saveUserProfile(jsonEncode(loginResult.user.toJson()));

      // update api token
      ApiService().updateToken(loginResult.token);

      notifyListeners();

      // start gps tracking only if already checked in
      // otherwise wait for user to check in
      if (_isCheckedIn) {
        BackgroundServiceUtils.startService();
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
      return await _attemptOfflineLogin(pin);
    }

    return true;
  }

  Future<bool> _attemptOfflineLogin(String pin) async {
    try {
      final savedHashedPin = await _storageService.getHashedPin();
      final savedUserProfile = await _storageService.getUserProfile();

      if (savedHashedPin == null || savedUserProfile == null) {
        setError('يجب تسجيل الدخول عبر الإنترنت لأول مرة');
        return false;
      }

      if (_hashPin(pin) != savedHashedPin) {
        setError('رقم الموظف غير صحيح');
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
    await _storageService.removeRefreshToken();

    try {
      await HiveInit.clearAllData();
    } catch (e) {
      if (kDebugMode) debugPrint('$e');
    }

    currentUser = null;
    jwtToken = null;
    _isCheckedIn = false;
    _activeAttendanceId = null;
    clearError();

    ApiService().updateToken(null);

    notifyListeners();
  }
}
