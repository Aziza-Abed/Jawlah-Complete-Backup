import 'package:flutter/foundation.dart';
import 'dart:io';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'package:uuid/uuid.dart';
import 'package:geolocator/geolocator.dart';

import '../data/models/issue_model.dart';
import '../data/models/local/issue_local.dart';
import '../data/services/issues_service.dart';
import '../data/repositories/local/issue_local_repository.dart';
import '../data/services/location_service.dart';
import '../core/utils/storage_helper.dart';
import 'sync_manager.dart';
import 'base_controller.dart';

// class for reporting problems in the feild
class IssueManager extends BaseController {
  final IssuesService _issuesService = IssuesService();
  final IssueLocalRepository _issueLocalRepo = IssueLocalRepository();
  SyncManager? _syncManager;

  List<IssueModel> myIssues = []; // all issues go here
  IssueModel? currentIssue; // issue user is looking at
  String? filterStatus;

  /// Clear all in-memory cached data (call on logout)
  void clearCache() {
    myIssues.clear();
    currentIssue = null;
    filterStatus = null;
    clearError();
    notifyListeners();
  }

  // set the sync manager
  void setSyncManager(SyncManager manager) {
    _syncManager = manager;
  }

  // send new issue report to server or save localy if offline
  // if position is provided uses it directly, otherwise fetches current GPS
  Future<bool> sendIssueReport({
    required String title,
    required String description,
    required String type,
    required String severity,
    String? locationDescription,
    required File photo1,
    File? photo2,
    File? photo3,
    Position? position,
  }) async {
    // get coordinates: use provided position or fetch current GPS
    double? lat;
    double? lng;
    if (position != null) {
      lat = position.latitude;
      lng = position.longitude;
    } else {
      try {
        final currentPos = await LocationService.getCurrentLocation()
            .timeout(const Duration(seconds: 10));
        if (currentPos != null) {
          lat = currentPos.latitude;
          lng = currentPos.longitude;
        }
      } catch (e) {
        if (kDebugMode) {
          debugPrint('GPS Fetch Error (reporting without coordinates): $e');
        }
      }
    }

    // try to send to server
    final success = await executeVoidWithErrorHandling(() async {
      final issue = await _issuesService.reportIssue(
        title: title,
        description: description,
        type: type,
        severity: severity,
        locationDescription: locationDescription,
        photo1: photo1,
        photo2: photo2,
        photo3: photo3,
        latitude: lat,
        longitude: lng,
      );

      myIssues.insert(0, issue);
    });

    if (success) return true;

    // if online failed save it localy on phone
    try {
      final user = await StorageHelper.takeUser();
      if (user == null) {
        setError('يجب تسجيل الدخول أولاً');
        return false;
      }

      // save photos to permanent location
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

        // copy extra photos if there
        if (photo2 != null) {
          final fileName2 = '${const Uuid().v4()}.jpg';
          final permanentPath2 = p.join(photoDir.path, fileName2);
          await photo2.copy(permanentPath2);
          permanentPaths.add(permanentPath2);
        }
        if (photo3 != null) {
          final fileName3 = '${const Uuid().v4()}.jpg';
          final permanentPath3 = p.join(photoDir.path, fileName3);
          await photo3.copy(permanentPath3);
          permanentPaths.add(permanentPath3);
        }
      } catch (e) {
        // fallback: use original path (may fail on sync if temp file deleted)
        if (permanentPaths.isEmpty) {
          permanentPaths = [photo1.path];
        }
      }

      // save to local db
      final issueLocal = IssueLocal(
        title: title,
        description: description,
        type: type,
        severity: severity,
        reportedByUserId: user.userId,
        latitude: lat ?? 0.0,
        longitude: lng ?? 0.0,
        locationDescription: locationDescription ?? '',
        photoUrl: permanentPaths.join(';'),
        reportedAt: DateTime.now(),
        createdAt: DateTime.now(),
        isSynced: false,
      );

      await _issueLocalRepo.addIssue(issueLocal);
      await _syncManager?.newDataAdded();

      // add temp item to list so user can see it
      final tempIssue = IssueModel(
        issueId: -1 * DateTime.now().millisecondsSinceEpoch,
        title: issueLocal.title,
        description: issueLocal.description,
        type: issueLocal.type,
        severity: issueLocal.severity,
        status: 'New',
        reportedByUserId: issueLocal.reportedByUserId,
        latitude: issueLocal.latitude != 0.0 ? issueLocal.latitude : null,
        longitude: issueLocal.longitude != 0.0 ? issueLocal.longitude : null,
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

  // load issues from server or local storage if offline
  Future<void> loadIssues({
    String? status,
    bool forceRefresh = false,
  }) async {
    if (isLoading && !forceRefresh) return;
    filterStatus = status;

    final isOnline = _syncManager?.isOnline ?? true;

    // if offline load from local storage
    if (!isOnline) {
      await _loadIssuesFromLocal(status);
      return;
    }

    // try to load from server
    final issueslist = await executeWithErrorHandling(
      () => _issuesService.getMyIssues(status: status),
    );

    if (issueslist != null) {
      myIssues = issueslist;
    } else {
      // only show offline mode if actually offline
      final actuallyOffline = !(_syncManager?.isOnline ?? true);
      if (actuallyOffline) {
        await _loadIssuesFromLocal(status);
      }
      // if online but server failed, error is already set by executeWithErrorHandling
    }
  }

  // load issues from local Hive storage when offline
  Future<void> _loadIssuesFromLocal(String? status) async {
    try {
      final user = await StorageHelper.takeUser();
      if (user == null) return;

      final localIssues = await _issueLocalRepo.getAllIssues(user.userId);
      List<IssueModel> issues = localIssues
          .map((local) => IssueModel.fromLocal(local))
          .toList();

      // apply status filter if given
      if (status != null) {
        issues = issues
            .where((i) => i.status.toLowerCase() == status.toLowerCase())
            .toList();
      }

      myIssues = issues;

      if (myIssues.isEmpty) {
        setError('لا توجد بلاغات محفوظة. الرجاء الاتصال بالإنترنت.');
      } else {
        setError('وضع عدم الاتصال - عرض ${myIssues.length} بلاغ محفوظ');
      }
    } catch (e) {
      if (kDebugMode) debugPrint('error loading issues from device: $e');
      setError('خطأ في قاعدة البيانات المحلية');
      myIssues = [];
    }

    setLoading(false);
    notifyListeners();
  }

  // get single issue by id
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

  // forward issue to a department
  Future<bool> forwardIssue({
    required int issueId,
    required int departmentId,
    String? notes,
  }) async {
    final result = await executeWithErrorHandling(
      () => _issuesService.forwardIssue(
        issueId: issueId,
        departmentId: departmentId,
        notes: notes,
      ),
    );

    if (result != null) {
      // Update the issue in the local list
      final index = myIssues.indexWhere((i) => i.issueId == issueId);
      if (index != -1) {
        myIssues[index] = result;
      }
      if (currentIssue?.issueId == issueId) {
        currentIssue = result;
      }
      return true;
    }
    return false;
  }

  // reset everything when user logs out
  void reset() {
    myIssues = [];
    currentIssue = null;
    filterStatus = null;
    clearError();
    setLoading(false);
    notifyListeners();
  }
}
