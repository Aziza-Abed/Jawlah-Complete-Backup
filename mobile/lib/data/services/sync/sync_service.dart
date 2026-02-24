import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../../core/config/api_config.dart';
import '../../../core/errors/app_exception.dart';
import '../device_service.dart';
import '../../models/local/task_local.dart';
import '../../models/local/issue_local.dart';
import '../../models/local/zone_local.dart';
import '../../repositories/local/attendance_local_repository.dart';
import '../../repositories/local/task_local_repository.dart';
import '../../repositories/local/issue_local_repository.dart';
import '../../repositories/local/zone_local_repository.dart';
import '../../../core/utils/date_formatter.dart';
import '../tasks_service.dart';
import '../issues_service.dart';

class SyncService {
  final Dio _dio;
  final AttendanceLocalRepository _attendanceLocalRepo;
  final TaskLocalRepository _taskLocalRepo;
  final IssueLocalRepository _issueLocalRepo;
  final ZoneLocalRepository _zoneLocalRepo;
  final TasksService _tasksService;
  final IssuesService _issuesService;

  SyncService(
    this._dio,
    this._attendanceLocalRepo,
    this._taskLocalRepo,
    this._issueLocalRepo,
    this._zoneLocalRepo,
    this._tasksService,
    this._issuesService,
  );

  // this method sends all local data attendance tasks issues to the server
  Future<SyncResult> syncToServer() async {
    final result = SyncResult();

    try {
      // sync attendance first
      final attendanceResult = await _syncAttendances();
      result.attendancesSynced = attendanceResult.success;
      result.attendancesFailed = attendanceResult.failed;

      // then sync tasks
      final taskResult = await _syncTasks();
      result.tasksSynced = taskResult.success;
      result.tasksFailed = taskResult.failed;

      // finaly sync issues
      final issueResult = await _syncIssues();
      result.issuesSynced = issueResult.success;
      result.issuesFailed = issueResult.failed;

      final hasErrors = (attendanceResult.error != null &&
              attendanceResult.error!.isNotEmpty) ||
          (taskResult.error != null && taskResult.error!.isNotEmpty) ||
          (issueResult.error != null && issueResult.error!.isNotEmpty);

      result.success = !hasErrors;

      if (hasErrors) {
        result.errorMessage = 'حدثت أخطاء أثناء المزامنة';
      }
    } catch (e) {
      result.success = false;
      result.errorMessage = e.toString();
    }

    return result;
  }

  Future<Response> _retryRequest(
    Future<Response> Function() request, {
    int maxRetries = 3,
  }) async {
    AppException? lastError;

    // retry loop try request up to maxRetries times with exponential backoff
    for (int attempt = 0; attempt < maxRetries; attempt++) {
      try {
        return await request();
      } catch (e) {
        if (e is AppException) {
          lastError = e;
        } else {
          lastError = ServerException(
              'حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
        }

        if (attempt < maxRetries - 1) {
          // Exponential backoff: 2s, 4s, 8s
          final delay = Duration(seconds: 2 << attempt);
          if (kDebugMode) {
            debugPrint('Sync retry attempt ${attempt + 1}, waiting ${delay.inSeconds}s');
          }
          await Future.delayed(delay);
        }
      }
    }

    throw lastError ??
        ServerException('حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقاً.');
  }

  Future<BatchResult> _syncAttendances() async {
    final result = BatchResult();

    try {
      final unsynced = await _attendanceLocalRepo.getUnsyncedAttendances();

      if (unsynced.isEmpty) {
        return result;
      }

      final items = unsynced.map((a) => a.toSyncDto()).toList();

      final response = await _retryRequest(() async {
        return await _dio.post(
          ApiConfig.syncAttendanceBatch,
          data: {
            'deviceId': await _getDeviceId(),
            'clientTime': DateTime.now().toIso8601String(),
            'items': items,
          },
        );
      });

      if (response.statusCode == 200) {
        final data = response.data['data'];
        final results = data?['results'] as List? ?? [];

        for (var syncResult in results) {
          if (syncResult['success']) {
            final clientId = syncResult['clientId'];
            final serverId = syncResult['serverId'];
            await _attendanceLocalRepo.markAsSynced(clientId, serverId);
            result.success++;
          } else {
            result.failed++;
          }
        }
      }
    } catch (e) {
      result.error = e.toString();
    }

    return result;
  }

  Future<BatchResult> _syncTasks() async {
    final result = BatchResult();

    try {
      final unsynced = await _taskLocalRepo.getUnsyncedTasks();

      if (unsynced.isEmpty) {
        return result;
      }

      final batchItems = <TaskLocal>[];

      for (var task in unsynced) {
        // use case insensitive comparison for status
        if (task.status.toLowerCase() == 'underreview' &&
            task.photoUrl != null &&
            task.photoUrl!.isNotEmpty &&
            !task.photoUrl!.startsWith('http')) {
          try {
            final file = File(task.photoUrl!);
            if (await file.exists()) {
              final updatedTask = await _tasksService.completeTask(
                task.taskId,
                notes: task.completionNotes ?? '',
                proofPhoto: file,
                latitude: task.latitude,
                longitude: task.longitude,
              );

              await _taskLocalRepo.markAsSynced(
                  task.taskId, updatedTask.syncVersion);
              result.success++;
            } else {
              batchItems.add(task);
            }
          } catch (e) {
            result.failed++;
          }
        } else {
          batchItems.add(task);
        }
      }

      if (batchItems.isNotEmpty) {
        final items = batchItems.map((t) => t.toSyncDto()).toList();

        final response = await _retryRequest(() async {
          return await _dio.post(
            ApiConfig.syncTasksBatch,
            data: {
              'deviceId': await _getDeviceId(),
              'clientTime': DateTime.now().toIso8601String(),
              'items': items,
            },
          );
        });

        if (response.statusCode == 200) {
          final data = response.data['data'];
          final results = data?['results'] as List? ?? [];

          for (var syncResult in results) {
            if (syncResult['success'] == true) {
              final taskId = syncResult['serverId'];
              final serverVersion = syncResult['serverVersion'] ?? 1;
              await _taskLocalRepo.markAsSynced(taskId, serverVersion);
              result.success++;
            } else if (syncResult['conflictResolution'] == 'ServerWins') {
              // Server has newer version - update local syncVersion so next
              // upload sends correct version instead of retrying forever
              final taskId = syncResult['serverId'] as int?;
              final serverVersion = syncResult['serverVersion'] as int? ?? 1;
              if (taskId != null) {
                await _taskLocalRepo.updateSyncVersion(taskId, serverVersion);
              }
              result.failed++;
            } else {
              result.failed++;
            }
          }
        }
      }
    } catch (e) {
      result.error = e.toString();
    }

    return result;
  }

  Future<BatchResult> _syncIssues() async {
    final result = BatchResult();

    try {
      final unsynced = await _issueLocalRepo.getUnsyncedIssues();

      if (unsynced.isEmpty) {
        return result;
      }

      for (var issue in unsynced) {
        try {
          File? p1, p2, p3;
          if (issue.photoUrl != null && issue.photoUrl!.isNotEmpty) {
            final paths = issue.photoUrl!.split(';');
            if (paths.isNotEmpty && await File(paths[0]).exists()) {
              p1 = File(paths[0]);
            }
            if (paths.length > 1 && await File(paths[1]).exists()) {
              p2 = File(paths[1]);
            }
            if (paths.length > 2 && await File(paths[2]).exists()) {
              p3 = File(paths[2]);
            }
          }

          if (p1 == null) {
            result.failed++;
            continue;
          }

          final newIssue = await _issuesService.reportIssue(
            title: issue.title,
            description: issue.description,
            type: issue.type,
            severity: issue.severity,
            locationDescription: issue.locationDescription,
            photo1: p1,
            photo2: p2,
            photo3: p3,
            latitude: issue.latitude,
            longitude: issue.longitude,
          );

          if (issue.clientId != null) {
            await _issueLocalRepo.markAsSynced(
                issue.clientId!, newIssue.issueId);
          }
          result.success++;
        } catch (e) {
          result.failed++;
        }
      }
    } catch (e) {
      result.error = e.toString();
    }

    return result;
  }

  // download latest data from server
  Future<void> syncFromServer() async {
    try {
      // get last sync time
      final lastSyncTime = await _getLastSyncTime();

      final response = await _dio.get(
        ApiConfig.syncChanges,
        queryParameters: {
          'lastSyncTime': lastSyncTime?.toIso8601String(),
        },
      );

      if (response.statusCode == 200) {
        final data = response.data['data'];
        if (data == null) return;

        // update tasks from server using field-level merge
        final tasks = data['tasks'] as List? ?? [];
        for (var taskJson in tasks) {
          final task = TaskLocal(
            taskId: taskJson['taskId'],
            title: taskJson['title'],
            status: taskJson['status'],
            completionNotes: taskJson['completionNotes'],
            photoUrl: taskJson['photoUrl'],
            completedAt: DateFormatter.tryParseUtc(taskJson['completedAt']),
            syncVersion: taskJson['syncVersion'] ?? 1,
            isSynced: true,
            updatedAt: DateTime.now(),
            syncedAt: DateTime.now(),
            description: taskJson['description'],
            priority: taskJson['priority'] ?? 'Medium',
            dueDate: DateFormatter.tryParseUtc(taskJson['dueDate']),
            zoneId: taskJson['zoneId'],
            zoneName: taskJson['zoneName'],
            latitude: (taskJson['latitude'] as num?)?.toDouble(),
            longitude: (taskJson['longitude'] as num?)?.toDouble(),
            locationDescription: taskJson['locationDescription'],
            taskType: taskJson['taskType'],
            requiresPhotoProof: taskJson['requiresPhotoProof'] ?? false,
            estimatedDurationMinutes: taskJson['estimatedDurationMinutes'],
            progressPercentage: taskJson['progressPercentage'] ?? 0,
            progressNotes: taskJson['progressNotes'],
            photos: (taskJson['photos'] as List?)?.map((e) => e.toString()).toList() ?? [],
          );
          // mergeFromServer handles conflict: if local has unsynced worker
          // changes, it keeps them and only takes supervisor fields from server
          await _taskLocalRepo.mergeFromServer(task);
        }

        // update issues from server using field-level merge
        final issues = data['issues'] as List? ?? [];
        for (var issueJson in issues) {
          final issue = IssueLocal(
            serverId: issueJson['issueId'],
            title: issueJson['title'],
            description: issueJson['description'],
            type: issueJson['type'],
            severity: issueJson['severity'],
            status: issueJson['status'] as String?,
            reportedByUserId: issueJson['reportedByUserId'],
            latitude: (issueJson['latitude'] as num?)?.toDouble() ?? 0.0,
            longitude: (issueJson['longitude'] as num?)?.toDouble() ?? 0.0,
            locationDescription: issueJson['locationDescription'],
            // Use photos array from server (joined with semicolons for IssueLocal compatibility)
            photoUrl: (issueJson['photos'] as List?)?.isNotEmpty == true
                ? (issueJson['photos'] as List).map((e) => e.toString()).join(';')
                : issueJson['photoUrl'],
            reportedAt: DateFormatter.tryParseUtc(issueJson['reportedAt']) ??
                DateTime.now(),
            isSynced: true,
            createdAt: DateTime.now(),
            syncedAt: DateTime.now(),
            syncVersion: issueJson['syncVersion'] as int? ?? 1,
            forwardedToDepartmentId: issueJson['forwardedToDepartmentId'] as int?,
            forwardedToDepartmentName: issueJson['forwardedToDepartmentName'] as String?,
            forwardedAt: DateFormatter.tryParseUtc(issueJson['forwardedAt']),
            forwardingNotes: issueJson['forwardingNotes'] as String?,
          );
          // mergeFromServer handles conflict: if local has unsynced worker
          // changes, it keeps them and only takes supervisor fields from server
          await _issueLocalRepo.mergeFromServer(issue);
        }

        // save server time for accurate sync (avoids clock drift)
        final serverTimeStr = data['currentServerTime'];
        final serverTime = DateFormatter.tryParseUtc(serverTimeStr) ?? DateTime.now();
        await _saveLastSyncTime(serverTime);
      }

      // sync zones for offline validation
      await syncZones();
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Sync failed: $e');
      }
    }
  }

  // download user's assigned zones for offline validation
  Future<void> syncZones() async {
    try {
      final response = await _dio.get(ApiConfig.myZones);

      if (response.statusCode == 200) {
        final responseData = response.data;
        if (responseData['success'] != true) return;

        final zonesJson = responseData['data'] as List? ?? [];
        final zones = zonesJson.map((json) => ZoneLocal.fromJson(json)).toList();

        await _zoneLocalRepo.saveZones(zones);

        if (kDebugMode) {
          debugPrint('Sync: Downloaded ${zones.length} zones for offline validation');
        }
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Zone sync failed: $e');
      }
    }
  }

  // use secure DeviceService for device ID (consistent with login)
  Future<String> _getDeviceId() async {
    return await DeviceService.getDeviceId();
  }

  Future<DateTime?> _getLastSyncTime() async {
    final prefs = await SharedPreferences.getInstance();
    final timestamp = prefs.getInt('last_sync_time');
    return timestamp != null
        ? DateTime.fromMillisecondsSinceEpoch(timestamp)
        : null;
  }

  Future<void> _saveLastSyncTime(DateTime time) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt('last_sync_time', time.millisecondsSinceEpoch);
  }

  // get count of pending sync items
  Future<int> getPendingSyncCount() async {
    final attendances = await _attendanceLocalRepo.getUnsyncedAttendances();
    final tasks = await _taskLocalRepo.getUnsyncedTasks();
    final issues = await _issueLocalRepo.getUnsyncedIssues();
    return attendances.length + tasks.length + issues.length;
  }
}

class SyncResult {
  bool success = false;
  String? errorMessage;
  int attendancesSynced = 0;
  int attendancesFailed = 0;
  int tasksSynced = 0;
  int tasksFailed = 0;
  int issuesSynced = 0;
  int issuesFailed = 0;

  int get totalSynced => attendancesSynced + tasksSynced + issuesSynced;
  int get totalFailed => attendancesFailed + tasksFailed + issuesFailed;
}

class BatchResult {
  int success = 0;
  int failed = 0;
  String? error;
}
