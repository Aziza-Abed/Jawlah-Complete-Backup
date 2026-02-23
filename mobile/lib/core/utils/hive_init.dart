import 'package:flutter/foundation.dart';
import 'package:hive_flutter/hive_flutter.dart';
import '../../data/models/local/attendance_local.dart';
import '../../data/models/local/task_local.dart';
import '../../data/models/local/issue_local.dart';
import '../../data/models/local/location_point.dart';
import '../../data/models/local/zone_local.dart';

class HiveInit {
  static Future<void> initialize() async {
    // initialize Hive for Flutter
    await Hive.initFlutter();

    // register adapters for our models
    Hive.registerAdapter(AttendanceLocalAdapter());
    Hive.registerAdapter(TaskLocalAdapter());
    Hive.registerAdapter(IssueLocalAdapter());
    Hive.registerAdapter(LocationPointAdapter());
    Hive.registerAdapter(ZoneLocalAdapter()); // Section 3.5.2: Offline zones

    // open boxes for local storage
    await Hive.openBox<AttendanceLocal>('attendance_local');
    await Hive.openBox<TaskLocal>('task_local');
    await Hive.openBox<IssueLocal>('issue_local');
    await Hive.openBox<LocationPoint>('location_buffer');
    await Hive.openBox<ZoneLocal>('zone_local'); // Section 3.5.2: Offline zones

    if (kDebugMode) debugPrint('Hive initialized successfully');
  }

  static Future<void> clearAllData() async {
    await Hive.box<AttendanceLocal>('attendance_local').clear();
    await Hive.box<TaskLocal>('task_local').clear();
    await Hive.box<IssueLocal>('issue_local').clear();
    await Hive.box<LocationPoint>('location_buffer').clear();
    await Hive.box<ZoneLocal>('zone_local').clear();
    if (kDebugMode) debugPrint('All local data cleared');
  }
}
