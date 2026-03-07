import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:logging/logging.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../../core/config/api_config.dart';
import '../../core/config/app_constants.dart';
import '../../core/utils/secure_storage_helper.dart';
import '../models/local/location_point.dart';

class TrackingService {
  HubConnection? _hubConnection;
  final Logger _logger = Logger('TrackingService');
  bool _isConnecting = false;

  // true while GPS points are queued for upload (offline buffering)
  static final ValueNotifier<bool> isBuffering = ValueNotifier(false);

  static final TrackingService _instance = TrackingService._internal();
  factory TrackingService() => _instance;
  TrackingService._internal();

  // initialize and connect to SignalR
  Future<void> connect() async {
    if (_hubConnection?.state == HubConnectionState.Connected ||
        _isConnecting) {
      return;
    }

    _isConnecting = true;
    try {
      final token = await SecureStorageHelper.getToken();

      if (token == null || token.isEmpty) {
        _logger.warning('No token found, cannot connect to tracking hub');
        return;
      }

      final hubUrl = ApiConfig.getHubUrl('/hubs/tracking');

      _hubConnection = HubConnectionBuilder()
          .withUrl(
            hubUrl,
            options: HttpConnectionOptions(
              accessTokenFactory: () async => await SecureStorageHelper.getToken() ?? '',
            ),
          )
          .configureLogging(Logger("SignalR"))
          .withAutomaticReconnect()
          .build();

      _hubConnection?.onclose(({Exception? error}) {
        _logger.info('Connection closed: $error');
        _isConnecting = false;
      });

      _hubConnection?.onreconnecting(({Exception? error}) {
        _logger.info('Reconnecting: $error');
      });

      _hubConnection?.onreconnected(({String? connectionId}) {
        _logger.info('Reconnected with ID: $connectionId');
        _flushBuffer();
      });

      await _hubConnection?.start();
      _logger.info('Tracking Hub Connected');

      _flushBuffer();
    } catch (e) {
      _logger.severe('Error connecting to Tracking Hub', e);
    } finally {
      _isConnecting = false;
    }
  }

  // stop connection
  Future<void> disconnect() async {
    await _hubConnection?.stop();
    _hubConnection = null;
  }

  // send location update to server
  Future<void> sendLocation(double lat, double lng,
      {double? speed, double? accuracy, double? heading}) async {
    final point = LocationPoint(
      latitude: lat,
      longitude: lng,
      timestamp: DateTime.now(),
      speed: speed,
      accuracy: accuracy,
      heading: heading,
    );

    if (_hubConnection?.state == HubConnectionState.Connected) {
      try {
        // build args list: always send lat/lng; append accuracy/speed/heading when available
        final args = <Object>[lat, lng];
        if (accuracy != null) {
          args.add(accuracy);
          args.add(speed ?? 0.0);
          args.add(heading ?? 0.0);
        }
        await _hubConnection?.invoke('SendLocationUpdate', args: args);
        _logger.fine('Location sent: $lat, $lng (acc: $accuracy, spd: $speed)');
      } catch (e) {
        _logger.warning('Failed to send location via SignalR, buffering...', e);
        await _bufferLocation(point);
      }
    } else {
      // SignalR not connected — send via REST API as fallback
      _logger.info('SignalR not connected, sending via REST fallback...');
      connect(); // attempt reconnect in background
      final sent = await _sendLocationViaRest(lat, lng,
          speed: speed, accuracy: accuracy, heading: heading);
      if (!sent) {
        await _bufferLocation(point);
      }
    }
  }

  // REST API fallback for when SignalR is unavailable
  Future<bool> _sendLocationViaRest(double lat, double lng,
      {double? speed, double? accuracy, double? heading}) async {
    try {
      final token = await SecureStorageHelper.getToken();
      if (token == null || token.isEmpty) return false;

      final dio = Dio(BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: const Duration(seconds: AppConstants.backgroundHttpTimeoutSeconds),
        receiveTimeout: const Duration(seconds: AppConstants.backgroundHttpTimeoutSeconds),
        headers: {
          'Content-Type': 'application/json; charset=utf-8',
          'Authorization': 'Bearer $token',
        },
      ));

      final response = await dio.post(
        'tracking/location',
        data: {
          'latitude': lat,
          'longitude': lng,
          if (speed != null) 'speed': speed,
          if (accuracy != null) 'accuracy': accuracy,
          if (heading != null) 'heading': heading,
          'timestamp': DateTime.now().toUtc().toIso8601String(),
        },
      );

      if (response.statusCode == 200) {
        _logger.fine('Location sent via REST fallback');
        return true;
      }
      return false;
    } catch (e) {
      _logger.warning('REST fallback also failed, buffering...', e);
      return false;
    }
  }

  // store point in Hive for later when offline
  Future<void> _bufferLocation(LocationPoint point) async {
    try {
      if (!Hive.isBoxOpen('location_buffer')) {
        await Hive.openBox<LocationPoint>('location_buffer');
      }
      final box = Hive.box<LocationPoint>('location_buffer');

      if (box.length > AppConstants.locationBufferMaxEntries) {
        final keysToDelete = box.keys.take(box.length - AppConstants.locationBufferMaxEntries).toList();
        await box.deleteAll(keysToDelete);
      }

      await box.add(point);
      isBuffering.value = true;
    } catch (e) {
      _logger.severe('Failed to buffer location', e);
    }
  }

  Future<void> _flushBuffer() async {
    if (!Hive.isBoxOpen('location_buffer')) return;

    final box = Hive.box<LocationPoint>('location_buffer');
    if (box.isEmpty) return;

    final points = box.values.toList();

    if (_hubConnection?.state == HubConnectionState.Connected) {
      List<dynamic> keysToDelete = [];

      for (var point in points) {
        try {
          final args = <Object>[point.latitude, point.longitude];
          if (point.accuracy != null) {
            args.add(point.accuracy!);
            args.add(point.speed ?? 0.0);
            args.add(point.heading ?? 0.0);
          }
          await _hubConnection?.invoke('SendLocationUpdate', args: args);
          keysToDelete.add(point.key);
        } catch (e) {
          break;
        }
      }

      await box.deleteAll(keysToDelete);
      if (box.isEmpty) isBuffering.value = false;
    }
  }

  void dispose() {
    disconnect();
  }
}
