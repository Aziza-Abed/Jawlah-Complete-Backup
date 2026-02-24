import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../data/models/attendance_model.dart';
import '../data/models/local/attendance_local.dart';
import '../data/services/attendance_service.dart';
import '../data/services/location_service.dart';
import '../data/services/zone_validation_service.dart';
import '../data/repositories/local/attendance_local_repository.dart';
import '../data/repositories/local/zone_local_repository.dart';
import 'sync_manager.dart';
import 'base_controller.dart';

// manager for check in and check out
class AttendanceManager extends BaseController {
  final AttendanceService _attendanceService = AttendanceService();
  final AttendanceLocalRepository _localRepo = AttendanceLocalRepository();
  SyncManager? _syncManager;

  AttendanceModel? todayRecord;
  List<AttendanceModel> myHistory = [];

  // pagination stuff
  int historyPage = 1;
  final int _pageSize = 20;
  bool canLoadMore = true;
  bool isFetchingMore = false;

  StreamSubscription? _bgSubscription;

  bool get isCheckedIn => todayRecord != null && todayRecord!.isActive;
  bool get isCheckedOut => todayRecord != null && !todayRecord!.isActive;
  bool get hasNotCheckedInToday => todayRecord == null;
  bool get isPendingApproval => todayRecord?.approvalStatus == 'Pending';
  String? get currentWorkDuration => todayRecord?.workDurationFormatted;

  // set sync manager
  void setSyncManager(SyncManager manager) {
    _syncManager = manager;
  }

  // listen for background service auto check-in/out events
  void listenToBackgroundUpdates() {
    _bgSubscription?.cancel();
    _bgSubscription = FlutterBackgroundService().on('attendanceUpdate').listen((event) {
      if (kDebugMode) debugPrint('Background attendance event: $event');
      loadTodayRecord();
    });
  }

  // cancel background listener
  void cancelBackgroundListener() {
    _bgSubscription?.cancel();
    _bgSubscription = null;
  }

  // request manual attendance (fallback - requires supervisor approval)
  Future<bool> requestManualAttendance(String reason, {int? zoneId}) async {
    final success = await executeVoidWithErrorHandling(() async {
      await _attendanceService.requestManualAttendance(
        reason: reason,
        zoneId: zoneId,
      );
      await loadTodayRecord();
    });
    return success;
  }


  // load today attendace from server or local
  Future<void> loadTodayRecord() async {
    final isOnline = _syncManager?.isOnline ?? true;

    if (isOnline) {
      final attendance = await executeWithErrorHandling(
        () => _attendanceService.getTodayAttendance(),
      );

      if (attendance != null) {
        todayRecord = attendance;

        // save to local for offline use
        final prefs = await SharedPreferences.getInstance();
        final userId = prefs.getInt('user_id');
        if (userId != null) {
          await _cacheTodayRecord(attendance, userId);
        }

        notifyListeners();
        return;
      }
    }

    // fallback to local if offline
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
          clearError();
        } else {
          // no local data and offline let user check in offline
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

  // do check in online or offline
  Future<bool> doCheckIn() async {
    final success = await executeVoidWithErrorHandling(() async {
      // try online check in first
      if (_syncManager?.isOnline ?? false) {
        try {
          final result = await _attendanceService.checkIn();
          todayRecord = result;

          // cache it localy
          final prefs = await SharedPreferences.getInstance();
          final userId = prefs.getInt('user_id');
          if (userId != null) {
            await _cacheTodayRecord(result, userId);
          }

          return;
        } catch (_) {
          // online failed - fall through to offline check-in
        }
      }

      // if offline save localy
      final prefs = await SharedPreferences.getInstance();
      final userId = prefs.getInt('user_id');

      if (userId == null || userId == 0) {
        throw Exception('يجب تسجيل الدخول أولاً');
      }

      // get gps location
      final position = await LocationService.getCurrentLocation();
      if (position == null) {
        throw Exception('يجب تفعيل تحديد الموقع (GPS)');
      }

      // Section 3.5.2: Offline zone validation using cached zones
      final zoneValidation = ZoneValidationService(ZoneLocalRepository());
      if (await zoneValidation.hasOfflineZones()) {
        final zone = await zoneValidation.validateLocationOffline(
          position.latitude,
          position.longitude,
        );
        if (zone == null) {
          throw Exception('أنت خارج منطقة العمل المخصصة لك');
        }
      }

      final localAttendance = AttendanceLocal(
        userId: userId,
        checkInTime: DateTime.now(),
        checkInLatitude: position.latitude,
        checkInLongitude: position.longitude,
        createdAt: DateTime.now(),
      );

      // save to local db and tell sync manager
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

    return success;
  }

  // do check out online or offline
  Future<bool> doCheckOut() async {
    final success = await executeVoidWithErrorHandling(() async {
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
        } catch (_) {
          // online failed - fall through to offline check-out
        }
      }

      // save on phone
      final prefs = await SharedPreferences.getInstance();
      final userId = prefs.getInt('user_id');

      if (userId == null || userId == 0) {
        throw Exception('يجب تسجيل الدخول أولاً');
      }

      final position = await LocationService.getCurrentLocation();
      if (position == null) {
        throw Exception('يجب تفعيل تحديد الموقع (GPS)');
      }

      final localAttendance = await _localRepo.getTodayAttendance(userId);
      if (localAttendance == null) {
        throw Exception('يجب تسجيل الحضور أولاً');
      }

      localAttendance.checkOutTime = DateTime.now();
      localAttendance.checkOutLatitude = position.latitude;
      localAttendance.checkOutLongitude = position.longitude;

      await _localRepo.updateAttendance(localAttendance);
      await _syncManager?.newDataAdded();

      // update ui
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

    return success;
  }

  // load attendace history
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

  // load more history rows
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

  // refresh today data
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

  // helper to save server record to local
  Future<void> _cacheTodayRecord(
      AttendanceModel serverRecord, int userId) async {
    try {
      // check if we alredy have local record
      var localRecord = await _localRepo.getTodayAttendance(userId);

      if (localRecord != null) {
        // update existing
        localRecord.serverId = serverRecord.attendanceId;
        localRecord.checkInTime = serverRecord.checkInTime;
        localRecord.checkOutTime = serverRecord.checkOutTime;
        localRecord.checkInLatitude = serverRecord.checkInLatitude;
        localRecord.checkInLongitude = serverRecord.checkInLongitude;
        localRecord.checkOutLatitude = serverRecord.checkOutLatitude;
        localRecord.checkOutLongitude = serverRecord.checkOutLongitude;
        localRecord.isValidated = serverRecord.isValid;
        localRecord.validationMessage = serverRecord.notes;
        localRecord.isSynced = true;
        localRecord.syncedAt = DateTime.now();

        await localRecord.save();
      } else {
        // create new record
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
