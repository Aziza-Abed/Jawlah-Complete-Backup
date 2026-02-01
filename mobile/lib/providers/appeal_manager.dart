import 'dart:io';

import '../data/models/appeal_model.dart';
import '../data/services/appeals_service.dart';
import 'base_controller.dart';

class AppealManager extends BaseController {
  final AppealsService _appealsService = AppealsService();

  List<AppealModel> myAppeals = [];
  AppealModel? currentAppeal;

  List<AppealModel> get pendingAppeals =>
      myAppeals.where((appeal) => appeal.isPending).toList();

  List<AppealModel> get approvedAppeals =>
      myAppeals.where((appeal) => appeal.isApproved).toList();

  List<AppealModel> get rejectedAppeals =>
      myAppeals.where((appeal) => appeal.isRejected).toList();

  int get pendingCount => pendingAppeals.length;
  int get approvedCount => approvedAppeals.length;
  int get rejectedCount => rejectedAppeals.length;

  bool get hasAppeals => myAppeals.isNotEmpty;

  /// Submit an appeal for an auto-rejected task
  Future<bool> submitAppeal({
    required int taskId,
    required String explanation,
    File? evidencePhoto,
  }) async {
    final success = await executeVoidWithErrorHandling(() async {
      await _appealsService.submitAppeal(
        taskId: taskId,
        explanation: explanation,
        evidencePhoto: evidencePhoto,
      );
    });

    if (success) {
      // Reload appeals list to show the new appeal
      await loadMyAppeals();
    }

    return success;
  }

  /// Load all appeals for current user
  Future<void> loadMyAppeals() async {
    final appeals = await executeWithErrorHandling(
      () => _appealsService.getMyAppeals(),
    );

    if (appeals != null) {
      myAppeals = appeals;
      notifyListeners();
    }
  }

  /// Load specific appeal by ID
  Future<void> loadAppealById(int appealId) async {
    final appeal = await executeWithErrorHandling(
      () => _appealsService.getAppealById(appealId),
    );

    if (appeal != null) {
      currentAppeal = appeal;

      // Update in list if exists
      final index = myAppeals.indexWhere((a) => a.appealId == appealId);
      if (index != -1) {
        myAppeals[index] = appeal;
      }

      notifyListeners();
    }
  }

  /// Check if an appeal exists for a specific task
  bool hasAppealForTask(int taskId) {
    return myAppeals.any((appeal) =>
        appeal.entityType == 'Task' &&
        appeal.entityId == taskId &&
        appeal.isPending);
  }

  /// Get appeal for a specific task
  AppealModel? getAppealForTask(int taskId) {
    try {
      return myAppeals.firstWhere(
        (appeal) =>
            appeal.entityType == 'Task' &&
            appeal.entityId == taskId,
      );
    } catch (e) {
      return null;
    }
  }

  void selectAppeal(AppealModel appeal) {
    currentAppeal = appeal;
    notifyListeners();
  }

  void clearSelection() {
    currentAppeal = null;
    notifyListeners();
  }

  Future<void> refreshData() async {
    await loadMyAppeals();
  }

  void reset() {
    myAppeals = [];
    currentAppeal = null;
    clearError();
    setLoading(false);
    notifyListeners();
  }
}
