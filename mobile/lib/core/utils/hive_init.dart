import 'package:flutter/foundation.dart';
import 'package:hive_flutter/hive_flutter.dart';
import '../../data/models/local/attendance_local.dart';
import '../../data/models/local/task_local.dart';
import '../../data/models/local/issue_local.dart';
import '../../data/models/local/location_point.dart';

class HiveInit {
  static Future<void> initialize() async {
    // initialize Hive for Flutter
    await Hive.initFlutter();

    // register adapters for our models
    Hive.registerAdapter(AttendanceLocalAdapter());
    Hive.registerAdapter(TaskLocalAdapter());
    Hive.registerAdapter(IssueLocalAdapter());
    Hive.registerAdapter(LocationPointAdapter());

    // open boxes for local storage
    await Hive.openBox<AttendanceLocal>('attendance_local');
    await Hive.openBox<TaskLocal>('task_local');
    await Hive.openBox<IssueLocal>('issue_local');
    await Hive.openBox<LocationPoint>('location_buffer');

    debugPrint('Hive initialized successfully');
  }

  static Future<void> clearAllData() async {
    await Hive.box<AttendanceLocal>('attendance_local').clear();
    await Hive.box<TaskLocal>('task_local').clear();
    await Hive.box<IssueLocal>('issue_local').clear();
    await Hive.box<LocationPoint>('location_buffer').clear();
    debugPrint('All local data cleared');
  }
}
