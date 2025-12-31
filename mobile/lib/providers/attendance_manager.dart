import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../data/models/attendance_model.dart';
import '../data/models/local/attendance_local.dart';
import '../data/services/attendance_service.dart';
import '../data/services/location_service.dart';
import '../data/repositories/local/attendance_local_repository.dart';
import 'sync_manager.dart';
import 'base_controller.dart';

// manager for worker check-in and check-out
class AttendanceManager extends BaseController {
  final AttendanceService _attendanceService = AttendanceService();
  final AttendanceLocalRepository _localRepo = AttendanceLocalRepository();
  SyncManager? _syncManager;

  AttendanceModel? todayRecord;
  List<AttendanceModel> myHistory = [];

  // pagination for history
  int historyPage = 1;
  final int _pageSize = 20;
  bool canLoadMore = true;
  bool isFetchingMore = false;

  bool get isCheckedIn => todayRecord != null && todayRecord!.isActive;
  bool get isCheckedOut => todayRecord != null && !todayRecord!.isActive;
  bool get hasNotCheckedInToday => todayRecord == null;
  String? get currentWorkDuration => todayRecord?.workDurationFormatted;

  // link sync manager
  void setSyncManager(SyncManager manager) {
    _syncManager = manager;
  }

  // load today attendance record from server or local storage
  Future<void> loadTodayRecord() async {
    final isOnline = _syncManager?.isOnline ?? true;

    if (isOnline) {
      final attendance = await executeWithErrorHandling(
        () => _attendanceService.getTodayAttendance(),
      );

      if (attendance != null) {
        todayRecord = attendance;

        // cache data locally for offline usage
        final prefs = await SharedPreferences.getInstance();
        final userId = prefs.getInt('user_id');
        if (userId != null) {
          await _cacheTodayRecord(attendance, userId);
        }

        notifyListeners();
        return;
      }
    }

    // fallback or offline: check local repo
    try {
      final prefs = await SharedPreferences.getInstance();
      final userId = prefs.getInt('user_id');
      if (userId != null && userId != 0) {
        final local = await _localRepo.getTodayAttendance(userId);
        if (local != null) {
          todayRecord = AttendanceModel(
            attendanceId: local.serverId ?? 0,
            userId: local.userId,
            checkInTime: local.checkInTime,
            checkOutTime: local.checkOutTime,
            checkInLatitude: local.checkInLatitude,
            checkInLongitude: local.checkInLongitude,
            checkOutLatitude: local.checkOutLatitude,
            checkOutLongitude: local.checkOutLongitude,
            isValid: local.isValidated,
            notes: local.validationMessage,
            createdAt: local.createdAt,
          );
          clearError(); // Clear "No internet" error if we have local data
        } else {
          // If no local data and offline, we don't want to show an error screen
          // We just want to allow the user to check-in offline
          if (!isOnline) {
            clearError();
          }
          todayRecord = null;
        }
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Local attendance load failed: $e');
    }
    notifyListeners();
  }

  // perform check-in (online or offline)
  Future<bool> doCheckIn() async {
    return await executeVoidWithErrorHandling(() async {
      // 1. try to check in online if we have internet
      if (_syncManager?.isOnline ?? false) {
        try {
          final result = await _attendanceService.checkIn();
          todayRecord = result;

          // cache it
          final prefs = await SharedPreferences.getInstance();
          final userId = prefs.getInt('user_id');
          if (userId != null) {
            await _cacheTodayRecord(result, userId);
          }
          return;
        } catch (e) {
          if (kDebugMode) debugPrint('Online check-in failed: $e');
        }
      }

      // 2. if offline or online fails, save it locally on the phone
      final prefs = await SharedPreferences.getInstance();
      final userId = prefs.getInt('user_id');

      if (userId == null || userId == 0) {
        throw Exception('يجب تسجيل الدخول أولاً');
      }

      // 3. get the GPS location
      final position = await LocationService.getCurrentLocation();
      if (position == null) {
        throw Exception('يجب تفعيل تحديد الموقع (GPS)');
      }

      final localAttendance = AttendanceLocal(
        userId: userId,
        checkInTime: DateTime.now(),
        checkInLatitude: position.latitude,
        checkInLongitude: position.longitude,
        createdAt: DateTime.now(),
      );

      // 4. save to the local database and tell sync manager to send it later
      await _localRepo.addAttendance(localAttendance);
      await _syncManager?.newDataAdded();

      todayRecord = AttendanceModel(
        attendanceId: 0,
        userId: userId,
        checkInTime: DateTime.now(),
        checkInLatitude: position.latitude,
        checkInLongitude: position.longitude,
        isValid: true,
        createdAt: DateTime.now(),
      );

      notifyListeners();
    });
  }

  // perform check-out (online or offline)
  Future<bool> doCheckOut() async {
    return await executeVoidWithErrorHandling(() async {
      // try online first
      if (_syncManager?.isOnline ?? false) {
        try {
          final result = await _attendanceService.checkOut();
          todayRecord = result;

          // cache it
          final prefs = await SharedPreferences.getInstance();
          final userId = prefs.getInt('user_id');
          if (userId != null) {
            await _cacheTodayRecord(result, userId);
          }
          return;
        } catch (e) {
          if (kDebugMode) debugPrint('Online check-out failed: $e');
        }
      }

      // save on device
      final prefs = await SharedPreferences.getInstance();
      final userId = prefs.getInt('user_id') ?? 0;

      final position = await LocationService.getCurrentLocation();
      if (position == null) {
        throw Exception('gps location required');
      }

      try {
        final localAttendance = await _localRepo.getTodayAttendance(userId);

        if (localAttendance != null) {
          localAttendance.checkOutTime = DateTime.now();
          localAttendance.checkOutLatitude = position.latitude;
          localAttendance.checkOutLongitude = position.longitude;

          await _localRepo.updateAttendance(localAttendance);
          await _syncManager?.newDataAdded();
        } else {
          throw Exception('يجب تسجيل الحضور أولاً');
        }
      } catch (e) {
        throw Exception('يجب تسجيل الحضور أولاً');
      }

      // update ui status
      if (todayRecord != null) {
        todayRecord = AttendanceModel(
          attendanceId: todayRecord!.attendanceId,
          userId: userId,
          checkInTime: todayRecord!.checkInTime,
          checkOutTime: DateTime.now(),
          checkInLatitude: todayRecord!.checkInLatitude,
          checkInLongitude: todayRecord!.checkInLongitude,
          checkOutLatitude: position.latitude,
          checkOutLongitude: position.longitude,
          isValid: true,
          createdAt: todayRecord!.createdAt,
        );
      }

      notifyListeners();
    });
  }

  // load attendance history list
  Future<void> loadHistory({
    DateTime? fromDate,
    DateTime? toDate,
  }) async {
    historyPage = 1;
    canLoadMore = true;

    final history = await executeWithErrorHandling(
      () => _attendanceService.getMyAttendance(
        fromDate: fromDate,
        toDate: toDate,
        page: historyPage,
        pageSize: _pageSize,
      ),
    );

    if (history != null) {
      myHistory = history;
      canLoadMore = history.length >= _pageSize;
      notifyListeners();
    }
  }

  // load more rows for history
  Future<void> loadExtraHistory({
    DateTime? fromDate,
    DateTime? toDate,
  }) async {
    if (isFetchingMore || !canLoadMore) return;

    isFetchingMore = true;
    notifyListeners();

    try {
      final moreHistory = await _attendanceService.getMyAttendance(
        fromDate: fromDate,
        toDate: toDate,
        page: historyPage + 1,
        pageSize: _pageSize,
      );

      if (moreHistory.isNotEmpty) {
        historyPage++;
        myHistory.addAll(moreHistory);
        canLoadMore = moreHistory.length >= _pageSize;
      } else {
        canLoadMore = false;
      }
    } catch (e) {
      canLoadMore = false;
    } finally {
      isFetchingMore = false;
      notifyListeners();
    }
  }

  // refresh today status
  Future<void> refreshTodayData() async {
    await loadTodayRecord();
  }

  // reset on logout
  void reset() {
    todayRecord = null;
    myHistory = [];
    clearError();
    setLoading(false);
    notifyListeners();
  }

  // helper to save server record to local storage
  Future<void> _cacheTodayRecord(
      AttendanceModel serverRecord, int userId) async {
    try {
      // 1. check if we already have a local record for today
      var localRecord = await _localRepo.getTodayAttendance(userId);

      if (localRecord != null) {
        // 2. update existing record
        localRecord.serverId = serverRecord.attendanceId;
        localRecord.checkInTime = serverRecord.checkInTime;
        localRecord.checkOutTime = serverRecord.checkOutTime;
        localRecord.checkInLatitude = serverRecord.checkInLatitude;
        localRecord.checkInLongitude = serverRecord.checkInLongitude;
        localRecord.checkOutLatitude = serverRecord.checkOutLatitude;
        localRecord.checkOutLongitude = serverRecord.checkOutLongitude;
        localRecord.isValidated = serverRecord.isValid;
        localRecord.validationMessage = serverRecord.notes;
        localRecord.isSynced = true; // it came from server, so it's synced
        localRecord.syncedAt = DateTime.now();

        await localRecord.save();
      } else {
        // 3. create new record if none exists
        final newRecord = AttendanceLocal(
          userId: userId,
          serverId: serverRecord.attendanceId,
          checkInTime: serverRecord.checkInTime,
          checkOutTime: serverRecord.checkOutTime,
          checkInLatitude: serverRecord.checkInLatitude,
          checkInLongitude: serverRecord.checkInLongitude,
          checkOutLatitude: serverRecord.checkOutLatitude,
          checkOutLongitude: serverRecord.checkOutLongitude,
          isValidated: serverRecord.isValid,
          validationMessage: serverRecord.notes,
          isSynced: true,
          createdAt: DateTime.now(),
          syncedAt: DateTime.now(),
        );

        await _localRepo.addAttendance(newRecord);
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Failed to cache attendance: $e');
    }
  }
}
