import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'package:uuid/uuid.dart';
import '../data/models/task_model.dart';
import '../data/services/tasks_service.dart';
import '../data/services/location_service.dart';
import '../data/repositories/local/task_local_repository.dart';

import 'sync_manager.dart';
import 'base_controller.dart';

class TaskManager extends BaseController {
  final TasksService _tasksService = TasksService();
  final TaskLocalRepository _taskLocalRepo = TaskLocalRepository();
  SyncManager? _syncManager;

  List<TaskModel> myTasks = []; // list to hold all tasks
  TaskModel? currentTask; // the task user is currently viewing

  String? filterStatus;
  String? filterPriority;

  // pagination state
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

  int get totalTasks => myTasks.length;
  int get pendingCount => pendingTasks.length;
  int get inProgressCount => inProgressTasks.length;
  int get completedCount => completedTasks.length;

  bool get hasTasks => myTasks.isNotEmpty;
  bool get isEmpty => myTasks.isEmpty;

  void setSyncManager(SyncManager manager) {
    _syncManager = manager;
  }

  // this method loads all tasks from server or local
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

    // if we are offline, just get tasks from the device
    if (!isOnline) {
      await getDataFromDevice(status, priority);
      return;
    }

    // if online, fetch them from the server
    try {
      final tasks = await _tasksService.getMyTasks(
        status: status,
        priority: priority,
        page: _currentPage,
        pageSize: _pageSize,
      );

      myTasks = tasks;
      _hasMoreData = tasks.length >= _pageSize;

      // save them to local storage for later use when offline
      _saveTasksToLocal(tasks).catchError((e) {
        if (kDebugMode) debugPrint('failed to save tasks locally: $e');
      });

      setLoading(false);
      setError(null);
      notifyListeners();
    } catch (e) {
      // if fetching fails (like network error), fallback to device data
      await getDataFromDevice(status, priority);
    }
  }

  // when offline, load tasks from phone storage
  Future<void> getDataFromDevice(String? status, String? priority) async {
    try {
      myTasks = await _loadLocalTasks(status: status, priority: priority);

      if (myTasks.isEmpty) {
        setError('لا توجد مهام محفوظة. الرجاء الاتصال بالإنترنت.');
      } else {
        // show message that we're offline
        setError('وضع عدم الاتصال - عرض ${myTasks.length} مهمة محفوظة');
      }
    } catch (e) {
      print('error loading from device: $e');
      setError('خطأ في قاعدة البيانات المحلية');
      myTasks = [];
    }

    setLoading(false);
    notifyListeners();
  }

  // get tasks from hive database
  Future<List<TaskModel>> _loadLocalTasks({
    String? status,
    String? priority,
  }) async {
    final localTasks = await _taskLocalRepo.getAllTasks();
    var tasks = localTasks.map((local) => TaskModel.fromLocal(local)).toList();
    return _applyFilters(tasks, status, priority);
  }

  // save tasks to phone for offline use
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

  // load more tasks when user scrolls down
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

      // save new tasks to local storage
      _saveTasksToLocal(moreTasks).catchError((e) {
        if (kDebugMode) debugPrint('failed to save tasks: $e');
      });
    } catch (e) {
      if (kDebugMode) debugPrint('error loading more tasks: $e');
      _currentPage--; // go back if failed
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

    // fallback to local list if offline or server fails
    final localTask = getTaskById(taskId);
    if (localTask != null) {
      currentTask = localTask;
      clearError(); // clear network errors if we have the data locally
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
      return await _saveStatusUpdateOffline(taskId, newStatus, notes: notes);
    }

    return success;
  }

  Future<bool> _saveStatusUpdateOffline(
    int taskId,
    String newStatus, {
    String? notes,
  }) async {
    final task = getTaskById(taskId);
    if (task == null) return false;

    try {
      // update task
      final updatedTask = task.copyWith(
        status: newStatus,
        completionNotes: notes,
        updatedAt: DateTime.now(),
      );

      // save to local database
      final taskLocal = updatedTask.toLocal();
      taskLocal.isSynced = false;
      await _taskLocalRepo.saveTask(taskLocal);

      // notify for auto-sync when online
      await _syncManager?.newDataAdded();

      // update state
      updateMyTaskInList(updatedTask);
      clearError(); // Clear "No Internet" error since we handled it offline
      notifyListeners();

      return true;
    } catch (e) {
      if (kDebugMode) debugPrint('offline status update failed: $e');
      return false;
    }
  }

  Future<bool> startWorkOnTask(int taskId) async {
    final success = await executeVoidWithErrorHandling(() async {
      final updatedTask = await _tasksService.startTask(taskId);
      updateMyTaskInList(updatedTask);
    });

    if (!success) {
      return await _saveStartedTaskOffline(taskId);
    }

    return success;
  }

  Future<bool> _saveStartedTaskOffline(int taskId) async {
    final task = getTaskById(taskId);
    if (task == null) return false;

    try {
      // update task
      final updatedTask = task.copyWith(
        status: 'InProgress',
        updatedAt: DateTime.now(),
      );

      // save to local database
      final taskLocal = updatedTask.toLocal();
      taskLocal.isSynced = false;
      await _taskLocalRepo.saveTask(taskLocal);

      // notify for auto-sync when online
      await _syncManager?.newDataAdded();

      // update state
      updateMyTaskInList(updatedTask);
      clearError();
      notifyListeners();

      return true;
    } catch (e) {
      if (kDebugMode) debugPrint('offline start failed: $e');
      return false;
    }
  }

  Future<bool> finishTask(
    int taskId, {
    required String notes,
    File? proofPhoto,
  }) async {
    final success = await executeVoidWithErrorHandling(() async {
      // get current location
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
    });

    // offline fallback - save locally
    if (!success) {
      return await _saveCompletedTaskOffline(taskId, notes, proofPhoto);
    }

    return success;
  }

  Future<bool> _saveCompletedTaskOffline(
    int taskId,
    String notes,
    File? proofPhoto,
  ) async {
    final task = getTaskById(taskId);
    if (task == null) return false;

    try {
      // save photo permanently if provided
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

      // save to local database
      final taskLocal = updatedTask.toLocal();
      taskLocal.isSynced = false;
      await _taskLocalRepo.saveTask(taskLocal);

      // notify for auto-sync when online
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

  // helper to update task in both list and selection
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

  // simple method to refresh the UI
  void refreshUI() {
    notifyListeners();
  }
}
