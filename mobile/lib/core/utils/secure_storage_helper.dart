import 'dart:convert';
import 'dart:math';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureStorageHelper {
  SecureStorageHelper._();

  static const String _keyToken = 'jwt_token';
  static const String _keyRefreshToken = 'refresh_token';
  static const String _keyFcmToken = 'fcm_token';
  static const String _keyHiveEncryption = 'hive_encryption_key';

  static const FlutterSecureStorage _secureStorage = FlutterSecureStorage(
    aOptions: AndroidOptions(
      encryptedSharedPreferences: true,
      resetOnError: true,
    ),
    iOptions: IOSOptions(
      accessibility: KeychainAccessibility.first_unlock,
    ),
  );

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

  // hive encryption
  static List<int> _generateEncryptionKey() {
    final random = Random.secure();
    return List<int>.generate(32, (_) => random.nextInt(256));
  }

  static Future<List<int>> getHiveEncryptionKey() async {
    try {
      final existingKey = await _secureStorage.read(key: _keyHiveEncryption);
      if (existingKey != null && existingKey.isNotEmpty) {
        return base64Decode(existingKey);
      }
      final newKey = _generateEncryptionKey();
      await _secureStorage.write(
        key: _keyHiveEncryption,
        value: base64Encode(newKey),
      );
      return newKey;
    } catch (e) {
      final fallbackKey = _generateEncryptionKey();
      return fallbackKey;
    }
  }

  // clear data (logout) - keeps hive key
  static Future<bool> clearAll() async {
    try {
      await _secureStorage.delete(key: _keyToken);
      await _secureStorage.delete(key: _keyRefreshToken);
      await _secureStorage.delete(key: _keyFcmToken);
      return true;
    } catch (e) {
      return false;
    }
  }
}
