import 'package:flutter_secure_storage/flutter_secure_storage.dart';

// helper class to store sensitive data like tokens securely
class SecureStorageHelper {
  SecureStorageHelper._();

  static const String _keyToken = 'jwt_token';
  static const String _keyRefreshToken = 'refresh_token';
  static const String _keyFcmToken = 'fcm_token';
  static const String _keyEmployeeId = 'employee_id';
  static const String _keyHashedPin = 'hashed_pin';

  static const FlutterSecureStorage _secureStorage = FlutterSecureStorage();

  // token methods
  static Future<bool> saveToken(String token) async {
    try {
      await _secureStorage.write(key: _keyToken, value: token);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<String?> getToken() async {
    return await _secureStorage.read(key: _keyToken);
  }

  static Future<bool> removeToken() async {
    try {
      await _secureStorage.delete(key: _keyToken);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<bool> hasToken() async {
    final token = await getToken();
    return token != null && token.isNotEmpty;
  }

  // refresh token methods
  static Future<bool> saveRefreshToken(String refreshToken) async {
    try {
      await _secureStorage.write(key: _keyRefreshToken, value: refreshToken);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<String?> getRefreshToken() async {
    return await _secureStorage.read(key: _keyRefreshToken);
  }

  static Future<bool> removeRefreshToken() async {
    try {
      await _secureStorage.delete(key: _keyRefreshToken);
      return true;
    } catch (e) {
      return false;
    }
  }

  // fcm methods
  static Future<bool> saveFcmToken(String fcmToken) async {
    try {
      await _secureStorage.write(key: _keyFcmToken, value: fcmToken);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<String?> getFcmToken() async {
    return await _secureStorage.read(key: _keyFcmToken);
  }

  static Future<bool> removeFcmToken() async {
    try {
      await _secureStorage.delete(key: _keyFcmToken);
      return true;
    } catch (e) {
      return false;
    }
  }

  // employee ID (PIN) methods - stored securely
  static Future<bool> saveEmployeeId(String employeeId) async {
    try {
      await _secureStorage.write(key: _keyEmployeeId, value: employeeId);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<String?> getEmployeeId() async {
    return await _secureStorage.read(key: _keyEmployeeId);
  }

  static Future<bool> removeEmployeeId() async {
    try {
      await _secureStorage.delete(key: _keyEmployeeId);
      return true;
    } catch (e) {
      return false;
    }
  }

  // hashed PIN for offline login
  static Future<bool> saveHashedPin(String hashedPin) async {
    try {
      await _secureStorage.write(key: _keyHashedPin, value: hashedPin);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<String?> getHashedPin() async {
    return await _secureStorage.read(key: _keyHashedPin);
  }

  static Future<bool> removeHashedPin() async {
    try {
      await _secureStorage.delete(key: _keyHashedPin);
      return true;
    } catch (e) {
      return false;
    }
  }

  // clear all data (logout) - keeps employeeId for "remember me" but clears session data
  static Future<bool> clearAll() async {
    try {
      await _secureStorage.delete(key: _keyToken);
      await _secureStorage.delete(key: _keyRefreshToken);
      await _secureStorage.delete(key: _keyFcmToken);
      await _secureStorage.delete(key: _keyHashedPin);
      await _secureStorage.delete(key: _keyUserProfile);
      // Note: employeeId is kept for "remember me" feature
      return true;
    } catch (e) {
      return false;
    }
  }

  // clear everything including employee ID (full logout)
  static Future<bool> clearAllIncludingCredentials() async {
    try {
      await _secureStorage.delete(key: _keyToken);
      await _secureStorage.delete(key: _keyRefreshToken);
      await _secureStorage.delete(key: _keyFcmToken);
      await _secureStorage.delete(key: _keyEmployeeId);
      await _secureStorage.delete(key: _keyHashedPin);
      await _secureStorage.delete(key: _keyUserProfile);
      return true;
    } catch (e) {
      return false;
    }
  }

  // user profile methods - stored securely
  static const String _keyUserProfile = 'user_profile';

  static Future<bool> saveUserProfile(String userJson) async {
    try {
      await _secureStorage.write(key: _keyUserProfile, value: userJson);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<String?> getUserProfile() async {
    return await _secureStorage.read(key: _keyUserProfile);
  }

  static Future<bool> removeUserProfile() async {
    try {
      await _secureStorage.delete(key: _keyUserProfile);
      return true;
    } catch (e) {
      return false;
    }
  }

  // generic methods for custom keys
  static Future<bool> saveString(String key, String value) async {
    try {
      await _secureStorage.write(key: key, value: value);
      return true;
    } catch (e) {
      return false;
    }
  }

  static Future<String?> getString(String key) async {
    return await _secureStorage.read(key: key);
  }

  static Future<bool> removeString(String key) async {
    try {
      await _secureStorage.delete(key: key);
      return true;
    } catch (e) {
      return false;
    }
  }
}
