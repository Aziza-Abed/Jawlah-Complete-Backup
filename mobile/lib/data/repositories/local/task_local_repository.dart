import 'package:flutter/foundation.dart';
import 'package:hive/hive.dart';
import 'package:uuid/uuid.dart';
import '../../models/local/task_local.dart';

class TaskLocalRepository {
  static const String _boxName = 'task_local';
  final _uuid = const Uuid();

  Future<Box<TaskLocal>> _openBox() async {
    return await Hive.openBox<TaskLocal>(_boxName);
  }

  // add or update task on the phone
  Future<void> saveTask(TaskLocal task) async {
    final box = await _openBox();

    // check if the task is already saved
    final existingIndex =
        box.values.toList().indexWhere((t) => t.taskId == task.taskId);

    if (existingIndex >= 0) {
      // update the existing task details
      final existing = box.getAt(existingIndex)!;
      existing.status = task.status;
      existing.completionNotes = task.completionNotes;
      existing.photoUrl = task.photoUrl;
      existing.completedAt = task.completedAt;
      existing.progressPercentage = task.progressPercentage;
      existing.progressNotes = task.progressNotes;
      existing.updatedAt = DateTime.now();
      existing.isSynced = task.isSynced;
      await existing.save();
    } else {
      // if it's new, give it a unique client id and save it
      if (!task.isSynced && task.clientId == null) {
        task.clientId = _uuid.v4();
      }
      task.updatedAt = DateTime.now();
      await box.add(task);
    }
  }

  // save tasks coming from server without overwriting local changes
  Future<void> saveFromServer(TaskLocal task) async {
    final box = await _openBox();
    final existingIndex =
        box.values.toList().indexWhere((t) => t.taskId == task.taskId);

    if (existingIndex >= 0) {
      final existing = box.getAt(existingIndex)!;
      if (!existing.isSynced) {
        return;
      }
      await box.putAt(existingIndex, task);
    } else {
      await box.add(task);
    }
  }

  // get task by server ID
  Future<TaskLocal?> getTaskById(int taskId) async {
    final box = await _openBox();
    try {
      return box.values.firstWhere((t) => t.taskId == taskId);
    } catch (e) {
      return null;
    }
  }

  // get all unsynced task updates
  Future<List<TaskLocal>> getUnsyncedTasks() async {
    final box = await _openBox();
    return box.values.where((t) => !t.isSynced).toList();
  }

  // mark task as synced
  Future<void> markAsSynced(int taskId, int newVersion) async {
    final box = await _openBox();
    // handle errors to prevent crash if record not found
    try {
      final task = box.values.firstWhere((t) => t.taskId == taskId);
      task.isSynced = true;
      task.syncVersion = newVersion;
      task.syncedAt = DateTime.now();
      await task.save();
    } catch (e) {
      // task may have already been synced or removed
      if (kDebugMode) debugPrint('Task $taskId not found in local DB: $e');
    }
  }

  // update task from server (after download)
  Future<void> updateFromServer(TaskLocal task) async {
    final box = await _openBox();
    final existingIndex =
        box.values.toList().indexWhere((t) => t.taskId == task.taskId);

    if (existingIndex >= 0) {
      // update existing
      await box.putAt(existingIndex, task);
    } else {
      // add new from server
      await box.add(task);
    }
  }

  // merge server data with local data using field-level ownership.
  //
  // field ownership (non-overlapping):
  // - worker fields (never overwritten by server unless fully synced):
  //   status, completionNotes, photoUrl, completedAt,
  //   latitude, longitude, progressPercentage, progressNotes
  // - supervisor fields (always accepted from server):
  //   title, description, priority, dueDate, zoneId, zoneName,
  //   locationDescription, taskType, requiresPhotoProof, estimatedDurationMinutes
  Future<void> mergeFromServer(TaskLocal serverTask) async {
    final box = await _openBox();
    final existingIndex =
        box.values.toList().indexWhere((t) => t.taskId == serverTask.taskId);

    if (existingIndex < 0) {
      // new task from server - just add it
      await box.add(serverTask);
      return;
    }

    final existing = box.getAt(existingIndex)!;

    if (existing.isSynced) {
      // no local changes - safe to overwrite entirely
      await box.putAt(existingIndex, serverTask);
      return;
    }

    // FIELD-LEVEL MERGE: local has unsynced worker changes
    // Accept supervisor-owned fields from server
    existing.title = serverTask.title;
    existing.description = serverTask.description;
    existing.priority = serverTask.priority;
    existing.dueDate = serverTask.dueDate;
    existing.zoneId = serverTask.zoneId;
    existing.zoneName = serverTask.zoneName;
    existing.locationDescription = serverTask.locationDescription;
    existing.taskType = serverTask.taskType;
    existing.requiresPhotoProof = serverTask.requiresPhotoProof;
    existing.estimatedDurationMinutes = serverTask.estimatedDurationMinutes;

    // Worker fields (status, completionNotes, photoUrl, completedAt,
    // latitude, longitude, progressPercentage, progressNotes) stay as-is

    // Update version so next upload sends correct syncVersion
    existing.syncVersion = serverTask.syncVersion;
    // Keep isSynced = false - worker changes still need uploading
    await existing.save();
  }

  // update only the syncVersion after a server conflict response.
  // keeps isSynced = false so worker changes retry with correct version.
  Future<void> updateSyncVersion(int taskId, int newVersion) async {
    final box = await _openBox();
    try {
      final task = box.values.firstWhere((t) => t.taskId == taskId);
      task.syncVersion = newVersion;
      await task.save();
    } catch (e) {
      if (kDebugMode) debugPrint('Task $taskId not found for version update');
    }
  }

  // get all tasks
  Future<List<TaskLocal>> getAllTasks() async {
    final box = await _openBox();
    return box.values.toList();
  }

  // clear all data (for logout)
  Future<void> clearAll() async {
    final box = await _openBox();
    await box.clear();
  }
}
