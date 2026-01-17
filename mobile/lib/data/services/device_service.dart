import 'package:uuid/uuid.dart';
import '../../core/utils/secure_storage_helper.dart';

// service to manage device identity for device binding security
class DeviceService {
  DeviceService._();

  static const String _keyDeviceId = 'device_unique_id';
  static String? _cachedDeviceId;

  // get unique device ID - creates one if doesn't exist
  // this ID is used for device binding security (prevents PIN theft)
  static Future<String> getDeviceId() async {
    // return cached value if available
    if (_cachedDeviceId != null) {
      return _cachedDeviceId!;
    }

    // try to get existing device ID from secure storage
    final existingId = await SecureStorageHelper.getString(_keyDeviceId);
    if (existingId != null && existingId.isNotEmpty) {
      _cachedDeviceId = existingId;
      return existingId;
    }

    // generate new device ID (only happens once per app install)
    final newDeviceId = const Uuid().v4();
    await SecureStorageHelper.saveString(_keyDeviceId, newDeviceId);
    _cachedDeviceId = newDeviceId;

    return newDeviceId;
  }

  // clear device ID (only for testing/debugging)
  static Future<void> clearDeviceId() async {
    _cachedDeviceId = null;
    await SecureStorageHelper.removeString(_keyDeviceId);
  }
}
