import 'package:hive_flutter/hive_flutter.dart';
import 'package:logging/logging.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../../core/config/api_config.dart';
import '../../core/utils/secure_storage_helper.dart';
import '../models/local/location_point.dart';

class TrackingService {
  HubConnection? _hubConnection;
  final Logger _logger = Logger('TrackingService');
  bool _isConnecting = false;

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
              accessTokenFactory: () async => token,
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

  // send location update
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
        await _hubConnection?.invoke('SendLocationUpdate', args: [lat, lng]);
        _logger.fine('Location sent: $lat, $lng');
      } catch (e) {
        _logger.warning('Failed to send location via SignalR, buffering...', e);
        await _bufferLocation(point);
      }
    } else {
      _logger.info('SignalR not connected, buffering location...');
      connect();
      await _bufferLocation(point);
    }
  }

  // store point in Hive for later
  Future<void> _bufferLocation(LocationPoint point) async {
    try {
      if (!Hive.isBoxOpen('location_buffer')) {
        final encryptionKey = await SecureStorageHelper.getHiveEncryptionKey();
        await Hive.openBox<LocationPoint>(
          'location_buffer',
          encryptionCipher: HiveAesCipher(encryptionKey),
        );
      }
      final box = Hive.box<LocationPoint>('location_buffer');

      if (box.length > 1000) {
        final keysToDelete = box.keys.take(box.length - 1000).toList();
        await box.deleteAll(keysToDelete);
      }

      await box.add(point);
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
          await _hubConnection?.invoke('SendLocationUpdate',
              args: [point.latitude, point.longitude]);
          keysToDelete.add(point.key);
        } catch (e) {
          break;
        }
      }

      await box.deleteAll(keysToDelete);
    }
  }

  void dispose() {
    disconnect();
  }
}
