import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../../data/models/user_model.dart';
import 'secure_storage_helper.dart';

class StorageHelper {
  StorageHelper._();

  static const String _keyUser = 'user_data';
  static const String _keyEmployeeId = 'employee_id';
  static const String _keyRememberMe = 'remember_me';
  static const String _keyLanguage = 'language';
  static const String _keyThemeMode = 'theme_mode';

  static SharedPreferences? prefs;

  static Future<void> startStorage() async {
    prefs ??= await SharedPreferences.getInstance();
  }

  static Future<SharedPreferences> get _instance async {
    if (prefs == null) {
      await startStorage();
    }
    return prefs!;
  }

  // secure storage wrappers
  static Future<bool> saveToken(String token) =>
      SecureStorageHelper.saveToken(token);
  static Future<String?> getToken() => SecureStorageHelper.getToken();
  static Future<bool> removeToken() => SecureStorageHelper.removeToken();
  static Future<bool> hasToken() => SecureStorageHelper.hasToken();

  static Future<bool> saveRefreshToken(String refreshToken) =>
      SecureStorageHelper.saveRefreshToken(refreshToken);
  static Future<String?> getRefreshToken() =>
      SecureStorageHelper.getRefreshToken();
  static Future<bool> removeRefreshToken() =>
      SecureStorageHelper.removeRefreshToken();

  // user profile methods
  static Future<bool> keepUser(UserModel user) async {
    try {
      final p = await _instance;
      final userJson = jsonEncode(user.toJson());
      return await p.setString(_keyUser, userJson);
    } catch (e) {
      return false;
    }
  }

  static Future<UserModel?> takeUser() async {
    try {
      final p = await _instance;
      final userJson = p.getString(_keyUser);
      if (userJson == null || userJson.isEmpty) return null;
      return UserModel.fromJson(jsonDecode(userJson));
    } catch (e) {
      return null;
    }
  }

  static Future<bool> removeUser() async {
    final p = await _instance;
    return await p.remove(_keyUser);
  }

  static Future<bool> hasUser() async {
    final user = await takeUser();
    return user != null;
  }

  // employee ID (PIN) methods - now using secure storage for security
  static Future<bool> saveEmployeeId(String employeeId) =>
      SecureStorageHelper.saveEmployeeId(employeeId);

  static Future<String?> getEmployeeId() =>
      SecureStorageHelper.getEmployeeId();

  static Future<bool> removeEmployeeId() =>
      SecureStorageHelper.removeEmployeeId();

  static Future<bool> saveRememberMe(bool value) async {
    final p = await _instance;
    return await p.setBool(_keyRememberMe, value);
  }

  static Future<bool> getRememberMe() async {
    final p = await _instance;
    return p.getBool(_keyRememberMe) ?? false;
  }

  static Future<bool> saveLanguage(String languageCode) async {
    final p = await _instance;
    return await p.setString(_keyLanguage, languageCode);
  }

  static Future<String> getLanguage() async {
    final p = await _instance;
    return p.getString(_keyLanguage) ?? 'ar';
  }

  static Future<bool> saveThemeMode(String mode) async {
    final p = await _instance;
    return await p.setString(_keyThemeMode, mode);
  }

  static Future<String> getThemeMode() async {
    final p = await _instance;
    return p.getString(_keyThemeMode) ?? 'light';
  }

  static Future<bool> wipeData() async {
    final p = await _instance;
    await SecureStorageHelper.clearAll();
    await removeUser();

    final rememberMe = await getRememberMe();
    if (!rememberMe) {
      await p.remove(_keyEmployeeId);
      await p.remove(_keyRememberMe);
    }
    return true;
  }

  // generic methods
  static Future<bool> saveString(String key, String value) async {
    final p = await _instance;
    return await p.setString(key, value);
  }

  static Future<String?> getString(String key) async {
    final p = await _instance;
    return p.getString(key);
  }

  static Future<bool> saveInt(String key, int value) async {
    final p = await _instance;
    return await p.setInt(key, value);
  }

  static Future<int?> getInt(String key) async {
    final p = await _instance;
    return p.getInt(key);
  }

  static Future<bool> remove(String key) async {
    final p = await _instance;
    return await p.remove(key);
  }
}
