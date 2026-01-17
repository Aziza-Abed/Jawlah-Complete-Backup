import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/foundation.dart';
import '../data/services/sync/sync_service.dart';
import 'base_controller.dart';

// this manager checks internet and syncs data to server
class SyncManager extends BaseController {
  final Connectivity _connectivity = Connectivity();
  final SyncService _syncService;

  bool _isOnline = true;
  int waitingItems = 0; // how many items need to be synced
  bool isSyncingNow = false;
  SyncResult? lastResult;
  StreamSubscription<List<ConnectivityResult>>? _subscription;

  SyncManager(this._syncService) {
    _initConnectivity();
    _monitorConnectivity();
  }

  bool get isOnline => _isOnline;

  // check if we have internet when app starts
  Future<void> _initConnectivity() async {
    try {
      final results = await _connectivity.checkConnectivity();
      _updateConnectionStatus(results);
      await _refreshWaitingCount();
    } catch (e) {
      if (kDebugMode) debugPrint('Error: $e');
    }
  }

  // listen for connection changes
  void _monitorConnectivity() {
    _subscription = _connectivity.onConnectivityChanged.listen(
      (List<ConnectivityResult> results) async {
        _updateConnectionStatus(results);

        // if we get internet back try to sync automaticly
        if (_isOnline && waitingItems > 0 && !isSyncingNow) {
          await startSync();
        }
      },
    );
  }

  // update online or offline status
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

  // count items that need syncing
  Future<void> _refreshWaitingCount() async {
    waitingItems = await _syncService.getPendingSyncCount();
    notifyListeners();
  }

  // start syncing data to server
  Future<SyncResult> startSync() async {
    if (isSyncingNow) {
      return lastResult ?? SyncResult();
    }

    // check if we have internet first
    if (!_isOnline) {
      final result = SyncResult();
      result.success = false;
      result.errorMessage = 'لا يوجد اتصال بالإنترنت';
      return result;
    }

    isSyncingNow = true;
    notifyListeners();

    try {
      // send local data to server
      final uploadResult = await _syncService.syncToServer();
      lastResult = uploadResult;

      // get new data from server
      await _syncService.syncFromServer();

      // update count
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

  // call this when user saves new data while offline
  Future<void> newDataAdded() async {
    await _refreshWaitingCount();

    // try sync if online
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
