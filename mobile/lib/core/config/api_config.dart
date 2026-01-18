class ApiConfig {
  ApiConfig._();

  // override with env variable API_BASE_URL if set
  static String get baseUrl {
    const envUrl = String.fromEnvironment('API_BASE_URL', defaultValue: '');
    if (envUrl.isNotEmpty) return envUrl;

    if (const bool.fromEnvironment('dart.vm.product')) {
      return 'https://api.jawlah.ps/api/';
    }

    // local IP for development
    return 'http://192.168.1.3:5000/api/';
  }

  // authentication endpoints
  static const String loginWithPin = 'auth/login-pin';
  static const String loginWithGPS =
      'auth/login-gps'; // legacy: login + auto check-in
  static const String profile = 'auth/profile';
  static const String logout = 'auth/logout';
  static const String registerFcmToken = 'auth/register-fcm-token';

  // tasks endpoints
  static const String myTasks = 'tasks/my-tasks';
  static const String taskDetails = 'tasks';
  static const String updateTaskStatus = 'tasks';
  static const String completeTask = 'tasks';

  // attendance endpoints
  static const String checkIn = 'attendance/checkin';
  static const String checkOut = 'attendance/checkout';
  static const String todayAttendance = 'attendance/today';
  static const String attendanceHistory = 'attendance/history';

  // issues endpoints
  static const String reportIssue = 'issues/report-with-photo';
  static const String myIssues = 'issues';

  // zones endpoints
  static const String activeZones = 'zones';
  static const String myZones = 'zones/my';

  // notifications endpoints
  static const String notifications = 'notifications';
  static const String unreadNotifications = 'notifications/unread';
  static const String unreadCount = 'notifications/unread-count';
  static const String markAsRead = 'notifications';
  static const String markAllAsRead = 'notifications/mark-all-read';

  static const int timeoutSeconds = 60;

  static const bool enableLogging =
      bool.fromEnvironment('API_LOGGING', defaultValue: false);

  static const int maxImageSize = 5 * 1024 * 1024;

  static String getHubUrl(String endpoint) {
    // hubs are mapped at root level, not under /api
    final rootUrl = baseUrl.endsWith('/api/')
        ? baseUrl.substring(0, baseUrl.length - 5)
        : (baseUrl.endsWith('/api')
            ? baseUrl.substring(0, baseUrl.length - 4)
            : baseUrl);
    return '$rootUrl$endpoint';
  }

  // helper methods for dynamic endpoints
  static String getMarkAsReadUrl(int notificationId) {
    return '$markAsRead/$notificationId/mark-read';
  }
}
