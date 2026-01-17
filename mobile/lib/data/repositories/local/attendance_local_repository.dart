import 'package:flutter/foundation.dart';
import 'package:hive/hive.dart';
import 'package:uuid/uuid.dart';
import '../../models/local/attendance_local.dart';

class AttendanceLocalRepository {
  static const String _boxName = 'attendance_local';
  final _uuid = const Uuid();

  // open the local database box
  Future<Box<AttendanceLocal>> _openBox() async {
    return await Hive.openBox<AttendanceLocal>(_boxName);
  }

  // add new attendance record to the phone when offline
  Future<String> addAttendance(AttendanceLocal attendance) async {
    final box = await _openBox();
    // generate a unique id for the record
    attendance.clientId = _uuid.v4();
    attendance.createdAt = DateTime.now();
    // save it to the local box
    await box.add(attendance);
    return attendance.clientId!;
  }

  // get all unsynced attendance records
  Future<List<AttendanceLocal>> getUnsyncedAttendances() async {
    final box = await _openBox();
    return box.values.where((a) => !a.isSynced).toList();
  }

  // get today's attendance for a user
  Future<AttendanceLocal?> getTodayAttendance(int userId) async {
    final box = await _openBox();
    final today = DateTime.now();
    final startOfDay = DateTime(today.year, today.month, today.day);
    final endOfDay = startOfDay.add(const Duration(days: 1));

    // handle errors to prevent crash if record not found
    try {
      return box.values.firstWhere(
        (a) =>
            a.userId == userId &&
            a.checkInTime.isAfter(startOfDay) &&
            a.checkInTime.isBefore(endOfDay),
      );
    } catch (e) {
      // no attendance found for today - return null instead of throwing
      debugPrint('No attendance found for user $userId today: $e');
      return null;
    }
  }

  // update attendance (for checkout)
  Future<void> updateAttendance(AttendanceLocal attendance) async {
    attendance.isSynced = false; // Mark as unsynced after update
    await attendance.save();
  }

  // mark attendance as synced
  Future<void> markAsSynced(String clientId, int serverId) async {
    final box = await _openBox();
    // handle errors to prevent crash if record not found
    try {
      final attendance = box.values.firstWhere((a) => a.clientId == clientId);
      attendance.serverId = serverId;
      attendance.isSynced = true;
      attendance.syncedAt = DateTime.now();
      await attendance.save();
    } catch (e) {
      debugPrint('Attendance $clientId not found in local DB: $e');
    }
  }

  // get all attendance records (for display)
  Future<List<AttendanceLocal>> getAllAttendances(int userId) async {
    final box = await _openBox();
    return box.values.where((a) => a.userId == userId).toList()
      ..sort((a, b) => b.checkInTime.compareTo(a.checkInTime));
  }

  // clear all data (for logout)
  Future<void> clearAll() async {
    final box = await _openBox();
    await box.clear();
  }
}
