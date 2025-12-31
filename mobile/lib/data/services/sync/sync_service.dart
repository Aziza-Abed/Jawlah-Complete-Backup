import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:uuid/uuid.dart';
import '../../../core/errors/app_exception.dart';

import '../../models/local/task_local.dart';
import '../../models/local/issue_local.dart';
import '../../repositories/local/attendance_local_repository.dart';
import '../../repositories/local/task_local_repository.dart';
import '../../repositories/local/issue_local_repository.dart';
import '../tasks_service.dart';
import '../issues_service.dart';

class SyncService {
  final Dio _dio;
  final AttendanceLocalRepository _attendanceLocalRepo;
  final TaskLocalRepository _taskLocalRepo;
  final IssueLocalRepository _issueLocalRepo;
  final TasksService _tasksService;
  final IssuesService _issuesService;

  SyncService(
    this._dio,
    this._attendanceLocalRepo,
    this._taskLocalRepo,
    this._issueLocalRepo,
    this._tasksService,
    this._issuesService,
  );

  // this method sends all local data (attendance, tasks, issues) to the server
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

      // finally sync issues
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

    // retry loop - try request up to maxRetries times
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
          // wait 3 seconds before retry
          await Future.delayed(Duration(seconds: 3));
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
          '/sync/attendance/batch',
          data: {
            'deviceId': await _getDeviceId(),
            'clientTime': DateTime.now().toIso8601String(),
            'items': items,
          },
        );
      });

      if (response.statusCode == 200) {
        final data = response.data['data'];
        final results = data['results'] as List;

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
        if (task.status == 'Completed' &&
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
            '/sync/tasks/batch',
            data: {
              'deviceId': await _getDeviceId(),
              'clientTime': DateTime.now().toIso8601String(),
              'items': items,
            },
          );
        });

        if (response.statusCode == 200) {
          final data = response.data['data'];
          final results = data['results'] as List;

          for (var syncResult in results) {
            if (syncResult['success']) {
              final taskId = syncResult['serverId'];
              final serverVersion = syncResult['serverVersion'];
              await _taskLocalRepo.markAsSynced(taskId, serverVersion);
              result.success++;
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
            description: issue.description,
            type: issue.type,
            photo1: p1,
            photo2: p2,
            photo3: p3,
            severity: issue.severity,
            latitude: issue.latitude,
            longitude: issue.longitude,
            locationDescription: issue.locationDescription,
          );

          await _issueLocalRepo.markAsSynced(issue.clientId!, newIssue.issueId);
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
        '/sync/changes',
        queryParameters: {
          'lastSyncTime': lastSyncTime?.toIso8601String(),
        },
      );

      if (response.statusCode == 200) {
        final data = response.data['data'];

        // update tasks
        final tasks = data['tasks'] as List;
        for (var taskJson in tasks) {
          final existingTask =
              await _taskLocalRepo.getTaskById(taskJson['taskId']);

          // skip if local task has unsynced changes
          if (existingTask != null && !existingTask.isSynced) {
            continue;
          }

          final task = TaskLocal(
            taskId: taskJson['taskId'],
            title: taskJson['title'],
            status: taskJson['status'],
            completionNotes: taskJson['completionNotes'],
            photoUrl: taskJson['photoUrl'],
            completedAt: taskJson['completedAt'] != null
                ? DateTime.parse(taskJson['completedAt'])
                : null,
            syncVersion: taskJson['syncVersion'],
            isSynced: true,
            updatedAt: DateTime.now(),
            syncedAt: DateTime.now(),
            description: taskJson['description'],
            priority: taskJson['priority'] ?? 'Medium',
            dueDate: taskJson['dueDate'] != null
                ? DateTime.parse(taskJson['dueDate'])
                : null,
            zoneId: taskJson['zoneId'],
            latitude: (taskJson['latitude'] as num?)?.toDouble(),
            longitude: (taskJson['longitude'] as num?)?.toDouble(),
            locationDescription: taskJson['locationDescription'],
          );
          await _taskLocalRepo.updateFromServer(task);
        }

        // update issues
        final issues = data['issues'] as List;
        for (var issueJson in issues) {
          final existingIssue =
              await _issueLocalRepo.getIssueByServerId(issueJson['issueId']);

          if (existingIssue != null && !existingIssue.isSynced) {
            continue;
          }

          final issue = IssueLocal(
            serverId: issueJson['issueId'],
            title: issueJson['title'],
            description: issueJson['description'],
            type: issueJson['type'],
            severity: issueJson['severity'],
            reportedByUserId: issueJson['reportedByUserId'],
            latitude: (issueJson['latitude'] as num?)?.toDouble() ?? 0.0,
            longitude: (issueJson['longitude'] as num?)?.toDouble() ?? 0.0,
            locationDescription: issueJson['locationDescription'],
            photoUrl: issueJson['photoUrl'],
            reportedAt: DateTime.parse(issueJson['reportedAt']),
            isSynced: true,
            createdAt: DateTime.now(),
            syncedAt: DateTime.now(),
          );
          await _issueLocalRepo.updateFromServer(issue);
        }

        // save new sync time
        await _saveLastSyncTime(DateTime.now());
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Sync failed: $e');
      }
    }
  }

  Future<String> _getDeviceId() async {
    final prefs = await SharedPreferences.getInstance();
    var deviceId = prefs.getString('device_id');

    if (deviceId == null) {
      const uuid = Uuid();
      deviceId = uuid.v4();
      await prefs.setString('device_id', deviceId);
    }
    return deviceId;
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
