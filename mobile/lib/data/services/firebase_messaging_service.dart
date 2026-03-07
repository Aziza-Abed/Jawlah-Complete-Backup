import 'dart:async';
import 'dart:convert';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../core/config/api_config.dart';
import '../../core/routing/app_router.dart';
import '../../core/utils/secure_storage_helper.dart';
import '../../main.dart' show navigatorKey;
import 'api_service.dart';

@pragma('vm:entry-point')
Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  if (kDebugMode) {
    debugPrint('Handling background message: ${message.messageId}');
    debugPrint('Title: ${message.notification?.title}');
    debugPrint('Body: ${message.notification?.body}');
  }
}

class FirebaseMessagingService {
  static final FirebaseMessagingService _instance =
      FirebaseMessagingService._internal();
  factory FirebaseMessagingService() => _instance;
  FirebaseMessagingService._internal();

  final FirebaseMessaging _firebaseMessaging = FirebaseMessaging.instance;
  final FlutterLocalNotificationsPlugin _localNotifications =
      FlutterLocalNotificationsPlugin();

  final _messageStreamController = StreamController<RemoteMessage>.broadcast();

  Stream<RemoteMessage> get onMessage => _messageStreamController.stream;

  String? _fcmToken;
  String? get fcmToken => _fcmToken;

  // Store subscriptions to prevent memory leaks
  StreamSubscription<String>? _tokenRefreshSubscription;
  StreamSubscription<RemoteMessage>? _onMessageSubscription;
  StreamSubscription<RemoteMessage>? _onMessageOpenedAppSubscription;

  // starting the notification service
  Future<void> initialize() async {
    if (kDebugMode) {
      debugPrint('Initializing Firebase Messaging...');
    }

    // request permission from user
    final settings = await _requestPermission();
    if (settings.authorizationStatus != AuthorizationStatus.authorized) {
      return;
    }

    // init local notifications
    await _initializeLocalNotifications();

    // get fcm token from firebase
    await _getFcmToken();

    // setup listeners for messages
    _setupMessageHandlers();

    // background handler
    FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);
  }

  Future<NotificationSettings> _requestPermission() async {
    return await _firebaseMessaging.requestPermission(
      alert: true,
      announcement: false,
      badge: true,
      carPlay: false,
      criticalAlert: false,
      provisional: false,
      sound: true,
    );
  }

  Future<void> _initializeLocalNotifications() async {
    const androidSettings =
        AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings();
    const settings =
        InitializationSettings(android: androidSettings, iOS: iosSettings);

    await _localNotifications.initialize(
      settings,
      onDidReceiveNotificationResponse: _onNotificationTapped,
    );
  }

  Future<void> _getFcmToken() async {
    try {
      _fcmToken = await _firebaseMessaging.getToken();
      if (kDebugMode) {
        debugPrint('FCM Token retrieved successfully');
      }

      if (_fcmToken != null) {
        await SecureStorageHelper.saveFcmToken(_fcmToken!);
      }

      // Cancel existing subscription before creating new one
      await _tokenRefreshSubscription?.cancel();
      _tokenRefreshSubscription = _firebaseMessaging.onTokenRefresh.listen((newToken) {
        if (kDebugMode) {
          debugPrint('FCM Token refreshed');
        }
        _fcmToken = newToken;
        _saveFcmToken(newToken);
      });
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error getting FCM token: $e');
      }
    }
  }

  Future<void> _saveFcmToken(String token) async {
    await SecureStorageHelper.saveFcmToken(token);

    await registerFcmTokenWithBackend(token);
  }

  Future<bool> registerFcmTokenWithBackend(String? token) async {
    if (token == null || token.isEmpty) {
      if (kDebugMode) {
        debugPrint('FCM token is null or empty, skipping registration');
      }
      return false;
    }

    try {
      final apiService = ApiService();
      final response = await apiService.post(
        ApiConfig.registerFcmToken,
        data: {'fcmToken': token},
      );

      if (response.statusCode == 200) {
        final responseData = response.data;
        if (responseData['success'] == true) {
          if (kDebugMode) {
            debugPrint('FCM token registered with backend successfully');
          }
          return true;
        }
      }

      if (kDebugMode) {
        debugPrint(
            'Failed to register FCM token with backend: ${response.statusCode}');
      }
      return false;
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error registering FCM token with backend: $e');
      }
      return false;
    }
  }

  // unregister FCM token from backend (called on logout)
  Future<bool> unregisterFcmToken() async {
    try {
      // clear local FCM token
      _fcmToken = null;
      await SecureStorageHelper.removeFcmToken();

      // tell backend to clear the token (send empty string)
      final apiService = ApiService();
      final response = await apiService.post(
        ApiConfig.registerFcmToken,
        data: {'fcmToken': ''},
      );

      if (response.statusCode == 200) {
        if (kDebugMode) {
          debugPrint('FCM token unregistered from backend successfully');
        }
        return true;
      }

      if (kDebugMode) {
        debugPrint('Failed to unregister FCM token: ${response.statusCode}');
      }
      return false;
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Error unregistering FCM token: $e');
      }
      return false;
    }
  }

  void _setupMessageHandlers() {
    // Cancel existing subscriptions before creating new ones
    _onMessageSubscription?.cancel();
    _onMessageOpenedAppSubscription?.cancel();

    _onMessageSubscription = FirebaseMessaging.onMessage.listen((RemoteMessage message) {
      if (kDebugMode) {
        debugPrint('Foreground message received: ${message.messageId}');
      }
      _messageStreamController.add(message);
      _showLocalNotification(message);
    });

    _onMessageOpenedAppSubscription = FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
      if (kDebugMode) {
        debugPrint('Notification tapped (background): ${message.messageId}');
      }
      _handleNotificationTap(message);
    });

    _firebaseMessaging.getInitialMessage().then((RemoteMessage? message) {
      if (message != null) {
        if (kDebugMode) {
          debugPrint('App opened from notification: ${message.messageId}');
        }
        _handleNotificationTap(message);
      }
    });
  }

  Future<void> _showLocalNotification(RemoteMessage message) async {
    final notification = message.notification;
    if (notification == null) return;

    // check user notification preferences
    final type = (message.data['type'] as String? ?? '').toLowerCase();
    final prefs = await SharedPreferences.getInstance();

    if (type.contains('task') && !(prefs.getBool('notif_tasks') ?? true)) return;
    if (type.contains('attendance') && !(prefs.getBool('notif_attendance') ?? true)) return;
    if (type.contains('issue') && !(prefs.getBool('notif_issues') ?? true)) return;
    if (type.contains('reminder') && !(prefs.getBool('notif_deadlines') ?? true)) return;

    const androidDetails = AndroidNotificationDetails(
      'followup_notifications',
      'FollowUp Notifications',
      channelDescription: 'Notifications for tasks, issues, and attendance',
      importance: Importance.high,
      priority: Priority.high,
      icon: '@mipmap/ic_launcher',
    );

    const iosDetails = DarwinNotificationDetails();

    const details =
        NotificationDetails(android: androidDetails, iOS: iosDetails);

    await _localNotifications.show(
      notification.hashCode,
      notification.title,
      notification.body,
      details,
      payload: jsonEncode(message.data),
    );
  }

  void _onNotificationTapped(NotificationResponse response) {
    if (kDebugMode) {
      debugPrint('Local notification tapped: ${response.payload}');
    }

    // try to navigate to the related task or issue
    if (response.payload != null && response.payload!.isNotEmpty) {
      try {
        final data = jsonDecode(response.payload!) as Map<String, dynamic>;
        final taskId = int.tryParse(data['taskId']?.toString() ?? '');
        final issueId = int.tryParse(data['issueId']?.toString() ?? '');

        if (taskId != null) {
          _navigateToScreen(Routes.taskDetails, taskId);
          return;
        }
        if (issueId != null) {
          _navigateToScreen(Routes.issueDetails, issueId);
          return;
        }
      } catch (_) {}
    }

    _navigateToScreen(Routes.notifications, null);
  }

  void _handleNotificationTap(RemoteMessage message) {
    if (kDebugMode) {
      debugPrint('FCM notification tapped: ${message.data}');
    }

    final notificationType = message.data['type'] as String?;
    final taskId = message.data['taskId'] as String?;
    final issueId = message.data['issueId'] as String?;

    if (kDebugMode) {
      debugPrint('Type: $notificationType, TaskId: $taskId, IssueId: $issueId');
    }

    switch (notificationType?.toLowerCase()) {
      // Task-related notifications
      case 'taskassigned':
      case 'task_assigned':
      case 'task_updated':
      case 'task_completed':
      case 'task_auto_rejected':
      case 'task_milestone':
      case 'task':
        if (taskId != null) {
          final id = int.tryParse(taskId);
          if (id != null) {
            _navigateToScreen(Routes.taskDetails, id);
            return;
          }
        }
        _navigateToScreen(Routes.tasksList, null);
        break;

      // Issue-related notifications
      case 'issuereported':
      case 'issue_reported':
      case 'issue_reviewed':
      case 'issue_forwarded':
      case 'issue':
        if (issueId != null) {
          final id = int.tryParse(issueId);
          if (id != null) {
            _navigateToScreen(Routes.issueDetails, id);
            return;
          }
        }
        _navigateToScreen(Routes.notifications, null);
        break;

      // Attendance notifications
      case 'attendancereminder':
      case 'attendance_reminder':
      case 'attendance':
        _navigateToScreen(Routes.attendance, null);
        break;

      // Warning notifications - go to notifications screen
      case 'warning_issued':
      case 'worker_warning_alert':
        _navigateToScreen(Routes.notifications, null);
        break;

      // Battery and system alerts - go to notifications screen
      case 'battery_low':
      case 'system_alert':
        _navigateToScreen(Routes.notifications, null);
        break;

      // Task extension request - go to task details if available
      case 'task_extension_request':
        if (taskId != null) {
          final id = int.tryParse(taskId);
          if (id != null) {
            _navigateToScreen(Routes.taskDetails, id);
            return;
          }
        }
        _navigateToScreen(Routes.notifications, null);
        break;

      default:
        _navigateToScreen(Routes.notifications, null);
    }
  }

  void _navigateToScreen(String route, dynamic arguments) {
    try {
      final navigator = navigatorKey.currentState;
      if (navigator != null) {
        navigator.pushNamed(route, arguments: arguments);
        if (kDebugMode) {
          debugPrint('Navigated to $route with args: $arguments');
        }
      } else {
        if (kDebugMode) {
          debugPrint('Navigator not available yet, storing pending navigation');
        }
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('Navigation error: $e');
      }
    }
  }

  void dispose() {
    // Cancel all subscriptions to prevent memory leaks
    _tokenRefreshSubscription?.cancel();
    _onMessageSubscription?.cancel();
    _onMessageOpenedAppSubscription?.cancel();
    _messageStreamController.close();
  }
}
