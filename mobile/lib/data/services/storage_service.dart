import '../../core/utils/storage_helper.dart';

// this service is for saving and reading data from the phone's memory
class StorageService {
  // singleton pattern
  static final StorageService _instance = StorageService._internal();
  factory StorageService() => _instance;
  StorageService._internal();

  // initialize storage
  Future<void> init() async {
    await StorageHelper.startStorage();
  }

  // token methods
  Future<bool> saveToken(String token) => StorageHelper.saveToken(token);
  Future<String?> getToken() => StorageHelper.getToken();
  Future<bool> removeToken() => StorageHelper.removeToken();

  // refresh token methods
  Future<bool> saveRefreshToken(String refreshToken) =>
      StorageHelper.saveRefreshToken(refreshToken);
  Future<String?> getRefreshToken() => StorageHelper.getRefreshToken();
  Future<bool> removeRefreshToken() => StorageHelper.removeRefreshToken();

  // user ID methods (save/get userId as int)
  Future<bool> saveUserId(int userId) =>
      StorageHelper.saveInt('user_id', userId);
  Future<int?> getUserId() => StorageHelper.getInt('user_id');

  // remember Me methods
  Future<bool> saveRememberMe(bool value) =>
      StorageHelper.saveRememberMe(value);
  Future<bool> getRememberMe() => StorageHelper.getRememberMe();

  // employee ID methods (for Remember Me)
  Future<bool> saveEmployeeId(String employeeId) =>
      StorageHelper.saveEmployeeId(employeeId);
  Future<String?> getEmployeeId() => StorageHelper.getEmployeeId();
  Future<bool> removeEmployeeId() => StorageHelper.removeEmployeeId();

  // offline login methods
  Future<bool> saveHashedPin(String hashedPin) =>
      StorageHelper.saveString('hashed_pin', hashedPin);
  Future<String?> getHashedPin() => StorageHelper.getString('hashed_pin');
  Future<bool> saveUserProfile(String userJson) =>
      StorageHelper.saveString('user_profile', userJson);
  Future<String?> getUserProfile() => StorageHelper.getString('user_profile');

  // clear all data
  Future<bool> clearAll() => StorageHelper.wipeData();
}
