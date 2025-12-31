import 'dart:convert';
import 'package:crypto/crypto.dart';
import 'package:flutter/foundation.dart';
import '../core/utils/hive_init.dart';
import '../core/utils/background_service_utils.dart';
import '../data/models/user_model.dart';
import '../data/services/auth_service.dart';
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

  UserModel? get user => currentUser;
  String? get token => jwtToken;
  bool get isAuthenticated => jwtToken != null && currentUser != null;
  String get userName => currentUser?.fullName ?? 'موظف';

  AuthManager(this._authService, this._storageService) {
    _loadSavedSession();
  }

  // load saved user session when the app starts
  Future<void> _loadSavedSession() async {
    try {
      // 1. check if we have a token and user id saved
      final token = await _storageService.getToken();
      final userId = await _storageService.getUserId();

      if (token != null && userId != null) {
        jwtToken = token;
        ApiService().updateToken(token);

        // 2. try to get the user profile from the server
        try {
          final user = await _authService.getProfile();
          currentUser = user;
        } catch (e) {
          // if it fails, maybe the token is old, so we clear everything
          jwtToken = null;
          currentUser = null;
          ApiService().updateToken(null);
          await _storageService.clearAll();
        }

        notifyListeners();

        // 3. start the background tracking if logged in
        if (jwtToken != null) {
          BackgroundServiceUtils.startService();
          await _registerFcmToken();
        }
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Error loading session: $e');
    }
  }

  String _hashPin(String pin) {
    return sha256.convert(utf8.encode(pin)).toString();
  }

  // login method
  Future<bool> doLogin(String pin) async {
    final result = await executeWithErrorHandling(() async {
      // 1. call the login service
      final loginResult = await _authService.loginWithGPS(employeeId: pin);

      currentUser = loginResult.user;
      jwtToken = loginResult.token;

      // 2. save everything to the phone storage
      await _storageService.saveToken(loginResult.token);
      await _storageService.saveUserId(loginResult.user.userId);
      if (loginResult.refreshToken != null &&
          loginResult.refreshToken!.isNotEmpty) {
        await _storageService.saveRefreshToken(loginResult.refreshToken!);
      }

      await _storageService.saveHashedPin(_hashPin(pin));
      await _storageService
          .saveUserProfile(jsonEncode(loginResult.user.toJson()));

      // 3. update the api client with the new token
      ApiService().updateToken(loginResult.token);

      notifyListeners();

      // 4. start background tracking
      BackgroundServiceUtils.startService();

      try {
        await _registerFcmToken();
      } catch (e) {
        // ignore if fcm fails
      }

      return loginResult;
    });

    if (result == null) {
      // 5. if online login fails (maybe no internet), try offline login
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
    clearError();

    ApiService().updateToken(null);

    notifyListeners();
  }
}
