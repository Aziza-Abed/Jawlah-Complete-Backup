import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/foundation.dart';
import '../data/services/sync/sync_service.dart';
import 'base_controller.dart';

// manager to handle internet connection and syncing data
class SyncManager extends BaseController {
  final Connectivity _connectivity = Connectivity();
  final SyncService _syncService;

  bool _isOnline = true;
  int waitingItems = 0; // count of items waiting to be sent to server
  bool isSyncingNow = false;
  SyncResult? lastResult;
  StreamSubscription<List<ConnectivityResult>>? _subscription;

  SyncManager(this._syncService) {
    _initConnectivity();
    _monitorConnectivity();
  }

  bool get isOnline => _isOnline;

  // check connection at start
  Future<void> _initConnectivity() async {
    try {
      final results = await _connectivity.checkConnectivity();
      _updateConnectionStatus(results);
      await _refreshWaitingCount();
    } catch (e) {
      if (kDebugMode) debugPrint('Error: $e');
    }
  }

  // watch for connection changes
  void _monitorConnectivity() {
    _subscription = _connectivity.onConnectivityChanged.listen(
      (List<ConnectivityResult> results) async {
        _updateConnectionStatus(results);

        // sync automatically when internet is back
        if (_isOnline && waitingItems > 0 && !isSyncingNow) {
          await startSync();
        }
      },
    );
  }

  // update the online/offline status
  void _updateConnectionStatus(List<ConnectivityResult> results) {
    final wasOnline = _isOnline;
    _isOnline = results.any((result) =>
        result == ConnectivityResult.mobile ||
        result == ConnectivityResult.wifi ||
        result == ConnectivityResult.ethernet);

    if (wasOnline != _isOnline) {
      notifyListeners();
    }
  }

  // count how many items need syncing
  Future<void> _refreshWaitingCount() async {
    waitingItems = await _syncService.getPendingSyncCount();
    notifyListeners();
  }

  // start the sync process
  Future<SyncResult> startSync() async {
    if (isSyncingNow) {
      return lastResult ?? SyncResult();
    }

    // check internet connection
    if (!_isOnline) {
      final result = SyncResult();
      result.success = false;
      result.errorMessage = 'لا يوجد اتصال بالإنترنت';
      return result;
    }

    isSyncingNow = true;
    notifyListeners();

    try {
      // upload local data
      final uploadResult = await _syncService.syncToServer();
      lastResult = uploadResult;

      // get latest updates
      await _syncService.syncFromServer();

      // update waiting count
      await _refreshWaitingCount();

      return uploadResult;
    } catch (e) {
      final result = SyncResult();
      result.success = false;
      result.errorMessage = e.toString();
      lastResult = result;
      return result;
    } finally {
      isSyncingNow = false;
      notifyListeners();
    }
  }

  // call this when user adds new data while offline
  Future<void> newDataAdded() async {
    await _refreshWaitingCount();

    // try to sync if online
    if (_isOnline && !isSyncingNow) {
      startSync();
    }
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }
}
