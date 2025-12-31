import 'package:flutter/foundation.dart';
import 'dart:io';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'package:uuid/uuid.dart';

import '../data/models/issue_model.dart';
import '../data/models/local/issue_local.dart';
import '../data/services/issues_service.dart';
import '../data/repositories/local/issue_local_repository.dart';
import '../data/services/location_service.dart';
import '../core/utils/storage_helper.dart';
import 'sync_manager.dart';
import 'base_controller.dart';

// class to manage reporting problems in the field
class IssueManager extends BaseController {
  final IssuesService _issuesService = IssuesService();
  final IssueLocalRepository _issueLocalRepo = IssueLocalRepository();
  SyncManager? _syncManager;

  List<IssueModel> myIssues = []; // list to hold all issues
  IssueModel? currentIssue; // the issue user is currently viewing
  String? filterStatus;

  // set sync manager
  void setSyncManager(SyncManager manager) {
    _syncManager = manager;
  }

  // report new issue (online with offline fallback)
  Future<bool> sendIssueReport({
    required String description,
    required String type,
    required File photo1,
    File? photo2,
    File? photo3,
  }) async {
    // 1. Get GPS location first, very safely
    double lat = 0.0;
    double lng = 0.0;
    try {
      final position = await LocationService.getCurrentLocation()
          .timeout(const Duration(seconds: 10));
      if (position != null) {
        lat = position.latitude;
        lng = position.longitude;
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('GPS Fetch Error (reporting anyway with 0,0): $e');
      }
    }

    // 2. try to send it to the server immediately
    final success = await executeVoidWithErrorHandling(() async {
      final issue = await _issuesService.reportIssue(
        description: description,
        type: type,
        photo1: photo1,
        photo2: photo2,
        photo3: photo3,
        latitude: lat != 0.0 ? lat : null,
        longitude: lng != 0.0 ? lng : null,
      );

      myIssues.insert(0, issue);
    });

    if (success) return true;

    // 3. if online failed, save it locally on the phone
    try {
      final user = await StorageHelper.takeUser();
      if (user == null) {
        setError('يجب تسجيل الدخول أولاً');
        return false;
      }

      // 4. save the photos to the phone's memory so they don't get deleted
      List<String> permanentPaths = [];
      try {
        final appDir = await getApplicationDocumentsDirectory();
        final photoDir = Directory(p.join(appDir.path, 'issue_photos'));
        if (!await photoDir.exists()) {
          await photoDir.create(recursive: true);
        }

        // copy photo1
        final fileName1 = '${const Uuid().v4()}.jpg';
        final permanentPath1 = p.join(photoDir.path, fileName1);
        await photo1.copy(permanentPath1);
        permanentPaths.add(permanentPath1);

        // copy extras if any
        if (photo2 != null) {
          final fileName2 = '${const Uuid().v4()}.jpg';
          final permanentPath2 = p.join(photoDir.path, fileName2);
          await photo2.copy(permanentPath2);
          permanentPaths.add(permanentPath2);
        }
      } catch (e) {
        permanentPaths = [photo1.path];
      }

      // 5. save to the local database
      final issueLocal = IssueLocal(
        title: '$type - بلاغ جديد',
        description: description,
        type: type,
        severity: 'Medium',
        reportedByUserId: user.userId,
        latitude: lat,
        longitude: lng,
        locationDescription: 'محفوظ محلياً',
        photoUrl: permanentPaths.join(';'),
        reportedAt: DateTime.now(),
        createdAt: DateTime.now(),
        isSynced: false,
      );

      await _issueLocalRepo.addIssue(issueLocal);
      await _syncManager?.newDataAdded();

      // 6. insert a temporary item in the list so the worker sees it
      final tempIssue = IssueModel(
        issueId: -1 * DateTime.now().millisecondsSinceEpoch,
        title: issueLocal.title,
        description: issueLocal.description,
        type: issueLocal.type,
        severity: issueLocal.severity,
        status: 'Reported',
        reportedByUserId: issueLocal.reportedByUserId,
        latitude: issueLocal.latitude,
        longitude: issueLocal.longitude,
        photoUrl: photo1.path,
        createdAt: issueLocal.createdAt,
        updatedAt: issueLocal.createdAt,
      );

      myIssues.insert(0, tempIssue);
      notifyListeners();
      clearError();
      return true;
    } catch (e) {
      setError('فشل حفظ البلاغ: ${e.toString()}');
      return false;
    }
  }

  // load issues from server
  Future<void> loadIssues({
    String? status,
    bool forceRefresh = false,
  }) async {
    if (isLoading && !forceRefresh) return;
    filterStatus = status;

    final issueslist = await executeWithErrorHandling(
      () => _issuesService.getMyIssues(status: status),
    );

    if (issueslist != null) {
      myIssues = issueslist;
    }
  }

  // get single issue details
  Future<void> getIssueDetails(int issueId) async {
    final issue = await executeWithErrorHandling(
      () => _issuesService.getIssueById(issueId),
    );

    if (issue != null) {
      currentIssue = issue;
      final index = myIssues.indexWhere((i) => i.issueId == issueId);
      if (index != -1) {
        myIssues[index] = currentIssue!;
      }
    }
  }

  void selectIssue(IssueModel issue) {
    currentIssue = issue;
    notifyListeners();
  }

  void clearSelection() {
    currentIssue = null;
    notifyListeners();
  }

  Future<void> refreshData() async {
    await loadIssues(status: filterStatus, forceRefresh: true);
  }

  // reset on logout
  void reset() {
    myIssues = [];
    currentIssue = null;
    filterStatus = null;
    clearError();
    setLoading(false);
    notifyListeners();
  }
}
