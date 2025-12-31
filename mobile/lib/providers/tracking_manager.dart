import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../core/config/api_config.dart';
import '../core/utils/secure_storage_helper.dart';
import '../data/models/worker_location.dart';
import 'base_controller.dart';

// manager to track where workers are in real-time
class TrackingManager extends BaseController {
  HubConnection? connection;
  final Map<int, WorkerLocation> workerPositions = {};
  bool _isConnected = false;

  Map<int, WorkerLocation> get workerLocations => workerPositions;
  List<WorkerLocation> get activeWorkers => workerPositions.values.toList();
  bool get isConnected => _isConnected;
  int get activeWorkerCount => workerPositions.length;

  // connect to the tracking server using SignalR
  Future<void> startConnecting() async {
    if (connection?.state == HubConnectionState.Connected) {
      return;
    }

    await executeVoidWithErrorHandling(() async {
      // get the token for authentication
      final token = await SecureStorageHelper.getToken();

      if (token == null || token.isEmpty) {
        throw Exception('يجب تسجيل الدخول أولاً');
      }

      final hubUrl = ApiConfig.getHubUrl('/hubs/tracking');

      // build the connection
      connection = HubConnectionBuilder()
          .withUrl(
            hubUrl,
            options: HttpConnectionOptions(
              accessTokenFactory: () async => token,
            ),
          )
          .withAutomaticReconnect()
          .build();

      // setup listeners for updates
      connection?.on('ReceiveLocationUpdate', onGetLocation);
      connection?.on('ReceiveUserStatus', onGetStatus);

      connection?.onclose(({Exception? error}) {
        _isConnected = false;
        notifyListeners();
      });

      connection?.onreconnected(({String? connectionId}) {
        _isConnected = true;
        _joinSupervisorsGroup();
        notifyListeners();
      });

      // start the connection
      await connection?.start();
      _isConnected = true;
      await _joinSupervisorsGroup();
      notifyListeners();
    });
  }

  // join group to see others
  Future<void> _joinSupervisorsGroup() async {
    try {
      await connection?.invoke('JoinSupervisorsGroup');
    } catch (e) {
      if (kDebugMode) debugPrint('Error: $e');
    }
  }

  // when someone moves
  void onGetLocation(List<dynamic>? arguments) {
    if (arguments == null || arguments.length < 3) return;

    try {
      final userId = arguments[0] as int;
      final latitude = (arguments[1] as num).toDouble();
      final longitude = (arguments[2] as num).toDouble();

      final existing = workerPositions[userId];
      if (existing != null) {
        workerPositions[userId] = existing.copyWith(
          latitude: latitude,
          longitude: longitude,
          lastUpdate: DateTime.now(),
        );
      } else {
        workerPositions[userId] = WorkerLocation(
          userId: userId,
          fullName: 'Worker #$userId',
          role: 'Worker',
          latitude: latitude,
          longitude: longitude,
          lastUpdate: DateTime.now(),
        );
      }

      notifyListeners();
    } catch (e) {
      if (kDebugMode) debugPrint('Error: $e');
    }
  }

  // when someone goes online/offline
  void onGetStatus(List<dynamic>? arguments) {
    if (arguments == null || arguments.length < 2) return;

    try {
      final userId = arguments[0] as int;
      final status = arguments[1] as String;

      if (status == 'offline') {
        workerPositions.remove(userId);
        notifyListeners();
      }
    } catch (e) {
      if (kDebugMode) debugPrint('Error: $e');
    }
  }

  // stop tracking
  Future<void> stopConnecting() async {
    await connection?.stop();
    connection = null;
    _isConnected = false;
    workerPositions.clear();
    notifyListeners();
  }

  // clear the list
  void clearWorkers() {
    workerPositions.clear();
    notifyListeners();
  }

  @override
  void dispose() {
    stopConnecting();
    super.dispose();
  }
}
