import 'package:flutter/foundation.dart';
import 'package:hive_flutter/hive_flutter.dart';
import '../../data/models/local/attendance_local.dart';
import '../../data/models/local/task_local.dart';
import '../../data/models/local/issue_local.dart';
import '../../data/models/local/location_point.dart';
import 'secure_storage_helper.dart';

class HiveInit {
  static Future<void> initialize() async {
    // initialize Hive for Flutter
    await Hive.initFlutter();

    // register adapters
    Hive.registerAdapter(AttendanceLocalAdapter());
    Hive.registerAdapter(TaskLocalAdapter());
    Hive.registerAdapter(IssueLocalAdapter());
    Hive.registerAdapter(LocationPointAdapter());

    // SECURITY: get encryption key from secure storage
    final encryptionKey = await SecureStorageHelper.getHiveEncryptionKey();
    final encryptionCipher = HiveAesCipher(encryptionKey);

    if (kDebugMode) {
      debugPrint('Hive encryption enabled (AES-256)');
    }

    // open boxes with encryption
    await Hive.openBox<AttendanceLocal>(
      'attendance_local',
      encryptionCipher: encryptionCipher,
    );
    await Hive.openBox<TaskLocal>(
      'task_local',
      encryptionCipher: encryptionCipher,
    );
    await Hive.openBox<IssueLocal>(
      'issue_local',
      encryptionCipher: encryptionCipher,
    );
    await Hive.openBox<LocationPoint>(
      'location_buffer',
      encryptionCipher: encryptionCipher,
    );

    debugPrint('Hive initialized successfully with encryption');
  }

  static Future<void> clearAllData() async {
    await Hive.box<AttendanceLocal>('attendance_local').clear();
    await Hive.box<TaskLocal>('task_local').clear();
    await Hive.box<IssueLocal>('issue_local').clear();
    await Hive.box<LocationPoint>('location_buffer').clear();
    debugPrint('All local data cleared');
  }
}
