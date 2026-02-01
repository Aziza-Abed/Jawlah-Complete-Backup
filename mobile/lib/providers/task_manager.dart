import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'package:uuid/uuid.dart';
import '../data/models/task_model.dart';
import '../data/services/tasks_service.dart';
import '../data/services/location_service.dart';
import '../data/repositories/local/task_local_repository.dart';
import '../core/errors/app_exception.dart';

import 'sync_manager.dart';
import 'base_controller.dart';

class TaskManager extends BaseController {
  final TasksService _tasksService = TasksService();
  final TaskLocalRepository _taskLocalRepo = TaskLocalRepository();
  SyncManager? _syncManager;

  List<TaskModel> myTasks = []; // all tasks list
  TaskModel? currentTask; // task user is looking at

  String? filterStatus;
  String? filterPriority;

  // pagination stuff
  int _currentPage = 1;
  final int _pageSize = 100;
  bool _hasMoreData = true;
  bool _isLoadingMore = false;

  bool get hasMoreData => _hasMoreData;
  bool get isLoadingMore => _isLoadingMore;

  List<TaskModel> get pendingTasks =>
      myTasks.where((task) => task.isPending).toList();
  List<TaskModel> get inProgressTasks =>
      myTasks.where((task) => task.isInProgress).toList();
  List<TaskModel> get completedTasks =>
      myTasks.where((task) => task.isCompleted).toList();
  List<TaskModel> get approvedTasks =>
      myTasks.where((task) => task.isApproved).toList();
  List<TaskModel> get rejectedTasks =>
      myTasks.where((task) => task.isRejected).toList();
  // reviewed = approved + rejected (tasks that supervisor has reviewed)
  List<TaskModel> get reviewedTasks =>
      myTasks.where((task) => task.isApproved || task.isRejected).toList();

  int get totalTasks => myTasks.length;
  int get pendingCount => pendingTasks.length;
  int get inProgressCount => inProgressTasks.length;
  int get completedCount => completedTasks.length;
  int get approvedCount => approvedTasks.length;
  int get rejectedCount => rejectedTasks.length;
  int get reviewedCount => reviewedTasks.length;
  // actionable = pending + inProgress + completed (excludes reviewed)
  int get actionableCount => pendingCount + inProgressCount + completedCount;

  bool get hasTasks => myTasks.isNotEmpty;
  bool get isEmpty => myTasks.isEmpty;

  void setSyncManager(SyncManager manager) {
    _syncManager = manager;
  }

  // load all tasks from server or local storage
  Future<void> loadTasks({
    String? status,
    String? priority,
    bool forceRefresh = false,
  }) async {
    if (isLoading && !forceRefresh) return;

    filterStatus = status;
    filterPriority = priority;
    _currentPage = 1;
    _hasMoreData = true;

    setLoading(true);
    setError(null);

    final isOnline = _syncManager?.isOnline ?? true;

    // if offline get tasks from phone
    if (!isOnline) {
      await getDataFromDevice(status, priority);
      return;
    }

    // if online get from server
    try {
      final tasks = await _tasksService.getMyTasks(
        status: status,
        priority: priority,
        page: _currentPage,
        pageSize: _pageSize,
      );

      myTasks = tasks;
      _hasMoreData = tasks.length >= _pageSize;

      // save to local for offline use later
      _saveTasksToLocal(tasks).catchError((e) {
        if (kDebugMode) debugPrint('failed to save tasks locally: $e');
      });

      setLoading(false);
      setError(null);
      notifyListeners();
    } catch (e) {
      // if server fails get from phone
      await getDataFromDevice(status, priority);
    }
  }

  // get tasks from phone storage when offline
  Future<void> getDataFromDevice(String? status, String? priority) async {
    try {
      myTasks = await _loadLocalTasks(status: status, priority: priority);

      if (myTasks.isEmpty) {
        setError('لا توجد مهام محفوظة. الرجاء الاتصال بالإنترنت.');
      } else {
        // show message that we offline
        setError('وضع عدم الاتصال - عرض ${myTasks.length} مهمة محفوظة');
      }
    } catch (e) {
      debugPrint('error loading from device: $e');
      setError('خطأ في قاعدة البيانات المحلية');
      myTasks = [];
    }

    setLoading(false);
    notifyListeners();
  }

  // get tasks from hive db
  Future<List<TaskModel>> _loadLocalTasks({
    String? status,
    String? priority,
  }) async {
    final localTasks = await _taskLocalRepo.getAllTasks();
    List<TaskModel> tasks = localTasks.map((local) => TaskModel.fromLocal(local)).toList();
    return _applyFilters(tasks, status, priority);
  }

  // save tasks to phone for offline
  Future<void> _saveTasksToLocal(List<TaskModel> tasks) async {
    for (var task in tasks) {
      await _taskLocalRepo.saveFromServer(task.toLocal());
    }
  }

  List<TaskModel> _applyFilters(
    List<TaskModel> tasks,
    String? status,
    String? priority,
  ) {
    var result = List<TaskModel>.from(tasks);

    if (status != null) {
      result = result
          .where((task) => task.status.toLowerCase() == status.toLowerCase())
          .toList();
    }

    if (priority != null) {
      result = result
          .where(
              (task) => task.priority.toLowerCase() == priority.toLowerCase())
          .toList();
    }

    return _sortByPriority(result);
  }

  List<TaskModel> _sortByPriority(List<TaskModel> tasks) {
    final sorted = List<TaskModel>.from(tasks);
    sorted.sort((a, b) {
      const priorityOrder = {'high': 0, 'medium': 1, 'low': 2, 'normal': 2};
      final aOrder = priorityOrder[a.priority.toLowerCase()] ?? 3;
      final bOrder = priorityOrder[b.priority.toLowerCase()] ?? 3;
      return aOrder.compareTo(bOrder);
    });
    return sorted;
  }

  // load more tasks when scrolling down
  Future<void> loadMoreTasks() async {
    if (_isLoadingMore || !_hasMoreData) return;

    _isLoadingMore = true;
    notifyListeners();

    try {
      _currentPage++;
      final moreTasks = await _tasksService.getMyTasks(
        status: filterStatus,
        priority: filterPriority,
        page: _currentPage,
        pageSize: _pageSize,
      );

      if (moreTasks.isEmpty || moreTasks.length < _pageSize) {
        _hasMoreData = false;
      }

      myTasks.addAll(moreTasks);

      // save new tasks localy
      _saveTasksToLocal(moreTasks).catchError((e) {
        if (kDebugMode) debugPrint('failed to save tasks: $e');
      });
    } catch (e) {
      if (kDebugMode) debugPrint('error loading more tasks: $e');
      _currentPage--;
    }

    _isLoadingMore = false;
    notifyListeners();
  }

  Future<void> getTaskDetails(int taskId) async {
    final isOnline = _syncManager?.isOnline ?? true;

    if (isOnline) {
      final task = await executeWithErrorHandling(
          () => _tasksService.getTaskById(taskId));

      if (task != null) {
        currentTask = task;
        updateMyTaskInList(task);
        return;
      }
    }

    // fallback to local if offline
    final localTask = getTaskById(taskId);
    if (localTask != null) {
      currentTask = localTask;
      clearError();
    } else if (!isOnline) {
      setError('المهمة غير موجودة في الذاكرة المحلية - يرجى الاتصال بالإنترنت');
    }
  }

  Future<bool> changeTaskStatus(
    int taskId,
    String newStatus, {
    String? notes,
  }) async {
    final success = await executeVoidWithErrorHandling(() async {
      final updatedTask = await _tasksService.updateTaskStatus(
        taskId,
        newStatus,
        notes: notes,
      );
      updateMyTaskInList(updatedTask);
    });

    if (!success) {
      return await _saveTaskOffline(taskId, newStatus, notes: notes);
    }

    return success;
  }

  // save task status change when offline
  Future<bool> _saveTaskOffline(int taskId, String newStatus,
      {String? notes}) async {
    final task = getTaskById(taskId);
    if (task == null) return false;

    try {
      final updatedTask = task.copyWith(
        status: newStatus,
        completionNotes: notes,
        updatedAt: DateTime.now(),
      );

      final taskLocal = updatedTask.toLocal();
      taskLocal.isSynced = false;
      await _taskLocalRepo.saveTask(taskLocal);
      await _syncManager?.newDataAdded();

      updateMyTaskInList(updatedTask);
      clearError();
      notifyListeners();
      return true;
    } catch (e) {
      if (kDebugMode) debugPrint('offline save failed: $e');
      return false;
    }
  }

  Future<bool> startWorkOnTask(int taskId) async {
    final success = await executeVoidWithErrorHandling(() async {
      final updatedTask = await _tasksService.startTask(taskId);
      updateMyTaskInList(updatedTask);
    });

    if (!success) {
      return await _saveTaskOffline(taskId, 'InProgress');
    }

    return success;
  }

  Future<bool> finishTask(
    int taskId, {
    required String notes,
    File? proofPhoto,
  }) async {
    try {
      setLoading(true);
      clearError();

      // get current gps location
      double? lat;
      double? lng;

      try {
        final position = await LocationService.getCurrentLocation();
        if (position != null) {
          lat = position.latitude;
          lng = position.longitude;
        }
      } catch (e) {
        if (kDebugMode) debugPrint('could not get location: $e');
      }

      final updatedTask = await _tasksService.completeTask(
        taskId,
        notes: notes,
        proofPhoto: proofPhoto,
        latitude: lat,
        longitude: lng,
      );

      updateMyTaskInList(updatedTask);
      setLoading(false);
      return true;
    } on AppException catch (e) {
      setLoading(false);

      // Check if this is a validation error (location mismatch)
      // These errors should NOT use offline fallback
      final isValidationError = e.message.contains('الموقع غير مطابق') ||
          e.message.contains('تحذير') ||
          e.message.contains('رفض') ||
          e.message.contains('المسافة');

      if (isValidationError) {
        // Don't use offline fallback for validation errors
        // Just set the error message and return false
        setError(e.message);
        return false;
      } else {
        // For network errors, use offline fallback
        setError(e.message);
        return await _saveCompletedTaskOffline(taskId, notes, proofPhoto);
      }
    } catch (e) {
      setLoading(false);

      // For unknown errors, try offline fallback
      final errorMessage = e.toString().replaceAll('Exception: ', '');
      setError(errorMessage);
      return await _saveCompletedTaskOffline(taskId, notes, proofPhoto);
    }
  }

  Future<bool> _saveCompletedTaskOffline(
    int taskId,
    String notes,
    File? proofPhoto,
  ) async {
    final task = getTaskById(taskId);
    if (task == null) return false;

    try {
      // save photo if there is one
      String? permanentPhotoPath;
      if (proofPhoto != null) {
        permanentPhotoPath = await _savePhotoLocally(proofPhoto);
      }

      // update task
      final updatedTask = task.copyWith(
        status: 'Completed',
        completionNotes: notes,
        photoUrl: permanentPhotoPath,
        completedAt: DateTime.now(),
        updatedAt: DateTime.now(),
      );

      // save to local db
      final taskLocal = updatedTask.toLocal();
      taskLocal.isSynced = false;
      await _taskLocalRepo.saveTask(taskLocal);

      // tell sync manager to sync later
      await _syncManager?.newDataAdded();

      // update state
      updateMyTaskInList(updatedTask);
      clearError();
      notifyListeners();

      return true;
    } catch (e) {
      if (kDebugMode) debugPrint('offline save failed: $e');
      return false;
    }
  }

  Future<String?> _savePhotoLocally(File photo) async {
    try {
      final appDir = await getApplicationDocumentsDirectory();
      final photoDir = Directory(p.join(appDir.path, 'task_photos'));

      if (!await photoDir.exists()) {
        await photoDir.create(recursive: true);
      }

      final fileName = '${const Uuid().v4()}.jpg';
      final permanentPath = p.join(photoDir.path, fileName);
      await photo.copy(permanentPath);

      return permanentPath;
    } catch (e) {
      if (kDebugMode) debugPrint('failed to save photo: $e');
      return photo.path;
    }
  }

  // helper to update task in list and current selection
  void updateMyTaskInList(TaskModel updatedTask) {
    final index = myTasks.indexWhere((t) => t.taskId == updatedTask.taskId);
    if (index != -1) {
      myTasks[index] = updatedTask;
    }

    if (currentTask?.taskId == updatedTask.taskId) {
      currentTask = updatedTask;
    }
  }

  void selectTask(TaskModel task) {
    currentTask = task;
    notifyListeners();
  }

  void clearSelection() {
    currentTask = null;
    notifyListeners();
  }

  Future<void> filterByStatus(String? status) async {
    await loadTasks(status: status, priority: filterPriority);
  }

  Future<void> filterByPriority(String? priority) async {
    await loadTasks(status: filterStatus, priority: priority);
  }

  Future<void> clearFilters() async {
    await loadTasks(status: null, priority: null);
  }

  Future<void> refreshData() async {
    await loadTasks(
      status: filterStatus,
      priority: filterPriority,
      forceRefresh: true,
    );
  }

  TaskModel? getTaskById(int taskId) {
    try {
      return myTasks.firstWhere((task) => task.taskId == taskId);
    } catch (e) {
      return null;
    }
  }

  // simple method to refresh ui
  void refreshUI() {
    notifyListeners();
  }

  // update task progress (for multi-day tasks)
  Future<void> updateTaskProgress(int taskId, int progressPercentage) async {
    await executeWithErrorHandling(() async {
      // call API to update progress
      final updated = await _tasksService.updateTaskProgress(
        taskId,
        progressPercentage,
      );

      if (updated != null) {
        // update in myTasks list
        final index = myTasks.indexWhere((t) => t.taskId == taskId);
        if (index != -1) {
          myTasks[index] = updated;
        }

        // update currentTask if this is the current task
        if (currentTask?.taskId == taskId) {
          currentTask = updated;
        }

        notifyListeners();
        return true;
      }

      throw ValidationException('فشل تحديث التقدم');
    });
  }
}
